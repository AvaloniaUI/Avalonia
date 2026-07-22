using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Persistent;
using NWayland;
using NWayland.Protocols.LinuxDmabufV1;
using NWayland.Protocols.Wayland;
using static Avalonia.OpenGL.Egl.EglConsts;
using static Avalonia.Wayland.Server.Interop.DrmGbmUnsafeNativeMethods;

namespace Avalonia.Wayland.Server.Transient.Rendering;

internal class WaylandEglDmaBufSurface : EglGlPlatformImageSurfaceBase, IPlatformRenderSurface
{
    private readonly WSurface _surface;

    public WaylandEglDmaBufSurface(WSurface surface)
    {
        _surface = surface;
    }

    public bool IsReady => _surface.State.IsReady;

    public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        => new RenderTarget(this, (EglContext)context);

    private class RenderTarget : EglPlatformImageSurfaceRenderTargetBase
    {
        private readonly WaylandEglDmaBufSurface _parent;
        private readonly WaylandEglDisplay _waylandDisplay;

        // TODO: Reduce to 2 or 3 if explicit sync is supported (once we implement it)
        private const int SwapchainSize = 4;
        private BufferSlot[]? _slots;
        private List<BufferSlot>? _pendingDestroy;
        private PixelSize _currentSize;
        private ulong _frameCounter;

        public RenderTarget(WaylandEglDmaBufSurface parent, EglContext context) : base(context)
        {
            _parent = parent;
            _waylandDisplay = (WaylandEglDisplay)context.Display;
            parent._surface.RegisterRenderTarget(this);
        }

        public override IGlPlatformSurfaceRenderingSession BeginDrawCore(
            IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            if (!_parent._surface.State.IsReady)
                throw new RenderTargetNotReadyException();

            EnsureSwapchain(sceneInfo.Size);

            // Select least recently used free slot
            BufferSlot? freeSlot = null;
            foreach (var slot in _slots!)
            {
                if (!slot.Busy && (freeSlot == null || slot.LastUsedFrame < freeSlot.LastUsedFrame))
                    freeSlot = slot;
            }

            if (freeSlot == null)
                throw new InvalidOperationException("No free buffer slots available in swapchain");

            var capturedSlot = freeSlot;
            capturedSlot.LastUsedFrame = ++_frameCounter;
            return BeginDraw(capturedSlot.EglImage!.Handle, sceneInfo.Size, sceneInfo.Scaling, () =>
            {
                // Stage per-frame state (frame callback + ack_configure +
                // geometry + viewport/scale + min/max) into the next
                // commit BEFORE binding the buffer, then commit explicitly
                // after attach + damage. The base WSurface no longer
                // issues the commit itself.
                _parent._surface.OnBeforeNewBufferAttached(sceneInfo);
                _parent._surface.WlSurface!.Attach(capturedSlot.WlBuffer!, 0, 0);
                _parent._surface.WlSurface.DamageBuffer(0, 0, sceneInfo.Size.Width, sceneInfo.Size.Height);
                capturedSlot.Busy = true;
                _parent._surface.WlSurface.Commit();
            });
        }

        public override PlatformRenderTargetState State =>
            IsCorrupted ? PlatformRenderTargetState.Corrupted
            : _disposed ? PlatformRenderTargetState.Disposed
            : _parent._surface.State;

        private bool _disposed;

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _parent._surface.UnregisterRenderTarget(this);
            DestroySwapchain();
            if (_pendingDestroy != null)
            {
                foreach (var slot in _pendingDestroy)
                    slot.Dispose();
                _pendingDestroy = null;
            }
            base.Dispose();
        }

        private unsafe void EnsureSwapchain(PixelSize size)
        {
            if (_slots != null && _currentSize == size)
                return;

            DestroySwapchain();

            _slots = new BufferSlot[SwapchainSize];
            _currentSize = size;

            var format = DRM_FORMAT_ARGB8888;

            // Collect modifiers for the chosen format
            var modifiers = _waylandDisplay.SupportedFormats
                .Where(f => f.Format == format)
                .Select(f => f.Modifier)
                .ToArray();

            for (var i = 0; i < SwapchainSize; i++)
            {
                var slot = new BufferSlot { Format = format };
                try
                {
                    // Allocate GBM buffer object
                    IntPtr bo = IntPtr.Zero;
                    if (modifiers.Length > 0)
                    {
                        fixed (ulong* modsPtr = modifiers)
                        {
                            bo = gbm_bo_create_with_modifiers2(
                                _waylandDisplay.GbmDevice, (uint)size.Width, (uint)size.Height,
                                format, modsPtr, (uint)modifiers.Length, GbmBoFlags.GBM_BO_USE_RENDERING);
                        }
                    }

                    if (bo == IntPtr.Zero)
                    {
                        bo = gbm_bo_create(
                            _waylandDisplay.GbmDevice, (uint)size.Width, (uint)size.Height,
                            format, GbmBoFlags.GBM_BO_USE_RENDERING);
                    }

                    if (bo == IntPtr.Zero)
                        throw new InvalidOperationException("Failed to create GBM buffer object");

                    slot.GbmBo = bo;

                    // Export DMA-BUF planes
                    var planeCount = gbm_bo_get_plane_count(bo);
                    slot.PlaneCount = planeCount;
                    slot.Modifier = gbm_bo_get_modifier(bo);
                    slot.DmaBufFds = new int[planeCount];
                    slot.Strides = new uint[planeCount];
                    slot.Offsets = new uint[planeCount];

                    for (var p = 0; p < planeCount; p++)
                    {
                        var handle = gbm_bo_get_handle_for_plane(bo, p);
                        if (drmPrimeHandleToFD(_waylandDisplay.DrmFd, handle.U32, 0, out slot.DmaBufFds[p]) != 0)
                            throw new InvalidOperationException($"drmPrimeHandleToFD failed for plane {p}");
                        slot.Strides[p] = gbm_bo_get_stride_for_plane(bo, p);
                        slot.Offsets[p] = gbm_bo_get_offset(bo, p);
                    }

                    // Create EGLImage from DMA-BUF
                    var attribs = new List<int>
                    {
                        EGL_WIDTH, (int)gbm_bo_get_width(bo),
                        EGL_HEIGHT, (int)gbm_bo_get_height(bo),
                        EGL_LINUX_DRM_FOURCC_EXT, (int)gbm_bo_get_format(bo),
                    };

                    for (var p = 0; p < planeCount; p++)
                        AddPlaneAttribs(attribs, p, slot.DmaBufFds[p], slot.Offsets[p],
                            slot.Strides[p], slot.Modifier);

                    attribs.Add(EGL_NONE);

                    var imageHandle = _waylandDisplay.EglInterface.CreateImageKHR(
                        _waylandDisplay.Handle, EGL_NO_CONTEXT,
                        EGL_LINUX_DMA_BUF_EXT, IntPtr.Zero, attribs.ToArray());

                    if (imageHandle == IntPtr.Zero)
                        throw new InvalidOperationException("eglCreateImageKHR failed");

                    slot.EglImage = new EglImage(_waylandDisplay, imageHandle);

                    // Create temp queue for roundtrip
                    using var tempQueue = _waylandDisplay.LinuxDmabuf.Display.CreateEventQueue();
                    // Create wl_buffer via zwp_linux_buffer_params_v1
                    var result = new DmaBufCreateParamsListener(new BufferReleaseListener(this, slot));
                    using var args = _waylandDisplay.LinuxDmabuf.CreateParams(result, tempQueue);
                    for (var p = 0; p < planeCount; p++)
                    {
                        args.Add(slot.DmaBufFds[p], (uint)p, slot.Offsets[p], slot.Strides[p],
                            (uint)(slot.Modifier >> 32), (uint)(slot.Modifier & 0xFFFFFFFF));
                    }
                    args.Create(size.Width, size.Height, format, default);
                    tempQueue.Roundtrip();
                    if (result.Buffer == null)
                        throw new OpenGlException("Unable to create DMA-BUF on Wayland server side");
                    slot.WlBuffer = result.Buffer;
                    // Put buffer back on our main queue
                    result.Buffer.SetQueue(_waylandDisplay.LinuxDmabuf.Queue);

                    _slots[i] = slot;
                }
                catch
                {
                    slot.Dispose();
                    throw;
                }
            }
        }

        class DmaBufCreateParamsListener(BufferReleaseListener bufferReleaseListener) : ZwpLinuxBufferParamsV1.Listener
        {
            public WlBuffer? Buffer { get; private set; } 
            protected override void Created(ZwpLinuxBufferParamsV1 eventSender, NewId<WlBuffer, WlBuffer.Listener> buffer)
            {
                Buffer = buffer.GetAndConsume(bufferReleaseListener);
            }
        }

        private static void AddPlaneAttribs(List<int> attribs, int plane, int fd,
            uint offset, uint stride, ulong modifier)
        {
            int baseFd, baseOffset, basePitch, baseModLo, baseModHi;
            switch (plane)
            {
                case 0:
                    baseFd = EGL_DMA_BUF_PLANE0_FD_EXT;
                    baseOffset = EGL_DMA_BUF_PLANE0_OFFSET_EXT;
                    basePitch = EGL_DMA_BUF_PLANE0_PITCH_EXT;
                    baseModLo = EGL_DMA_BUF_PLANE0_MODIFIER_LO_EXT;
                    baseModHi = EGL_DMA_BUF_PLANE0_MODIFIER_HI_EXT;
                    break;
                case 1:
                    baseFd = EGL_DMA_BUF_PLANE1_FD_EXT;
                    baseOffset = EGL_DMA_BUF_PLANE1_OFFSET_EXT;
                    basePitch = EGL_DMA_BUF_PLANE1_PITCH_EXT;
                    baseModLo = EGL_DMA_BUF_PLANE1_MODIFIER_LO_EXT;
                    baseModHi = EGL_DMA_BUF_PLANE1_MODIFIER_HI_EXT;
                    break;
                case 2:
                    baseFd = EGL_DMA_BUF_PLANE2_FD_EXT;
                    baseOffset = EGL_DMA_BUF_PLANE2_OFFSET_EXT;
                    basePitch = EGL_DMA_BUF_PLANE2_PITCH_EXT;
                    baseModLo = EGL_DMA_BUF_PLANE2_MODIFIER_LO_EXT;
                    baseModHi = EGL_DMA_BUF_PLANE2_MODIFIER_HI_EXT;
                    break;
                case 3:
                    baseFd = EGL_DMA_BUF_PLANE3_FD_EXT;
                    baseOffset = EGL_DMA_BUF_PLANE3_OFFSET_EXT;
                    basePitch = EGL_DMA_BUF_PLANE3_PITCH_EXT;
                    baseModLo = EGL_DMA_BUF_PLANE3_MODIFIER_LO_EXT;
                    baseModHi = EGL_DMA_BUF_PLANE3_MODIFIER_HI_EXT;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(plane));
            }

            attribs.Add(baseFd);
            attribs.Add(fd);
            attribs.Add(baseOffset);
            attribs.Add((int)offset);
            attribs.Add(basePitch);
            attribs.Add((int)stride);

            if (modifier != DRM_FORMAT_MOD_INVALID)
            {
                attribs.Add(baseModLo);
                attribs.Add((int)(modifier & 0xFFFFFFFF));
                attribs.Add(baseModHi);
                attribs.Add((int)(modifier >> 32));
            }
        }

        private void DestroySwapchain()
        {
            if (_slots == null)
                return;
            foreach (var slot in _slots)
            {
                if (slot == null)
                    continue;
                if (slot.Busy)
                {
                    // Defer destruction of busy buffers until compositor releases them
                    _pendingDestroy ??= new List<BufferSlot>();
                    _pendingDestroy.Add(slot);
                }
                else
                {
                    slot.Dispose();
                }
            }
            _slots = null;
            _currentSize = default;
        }

        private void OnBufferReleased(BufferSlot slot)
        {
            slot.Busy = false;
            if (_pendingDestroy != null && _pendingDestroy.Remove(slot))
                slot.Dispose();
        }

        private class BufferSlot : IDisposable
        {
            public IntPtr GbmBo;
            public int[]? DmaBufFds;
            public uint[]? Strides;
            public uint[]? Offsets;
            public int PlaneCount;
            public ulong Modifier;
            public uint Format;
            public EglImage? EglImage;
            public WlBuffer? WlBuffer;
            public bool Busy;
            public ulong LastUsedFrame;

            public void Dispose()
            {
                EglImage?.Dispose();
                EglImage = null;

                if (WlBuffer != null)
                {
                    WlBuffer.Destroy();
                    WlBuffer.Dispose();
                    WlBuffer = null;
                }

                if (DmaBufFds != null)
                {
                    foreach (var fd in DmaBufFds)
                    {
                        if (fd >= 0)
                            close(fd);
                    }

                    DmaBufFds = null;
                }

                if (GbmBo != IntPtr.Zero)
                {
                    gbm_bo_destroy(GbmBo);
                    GbmBo = IntPtr.Zero;
                }
            }
        }

        private class BufferReleaseListener(RenderTarget renderTarget, BufferSlot slot) : WlBuffer.Listener
        {
            protected override void Release(WlBuffer eventSender)
            {
                renderTarget.OnBufferReleased(slot);
            }
        }
    }
}
