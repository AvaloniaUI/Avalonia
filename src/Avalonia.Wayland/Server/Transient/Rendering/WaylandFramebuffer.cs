using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using NWayland.Protocols.Wayland;
using static Avalonia.Wayland.Server.Interop.UnsafeNativeMethods;
namespace Avalonia.Wayland.Server.Transient.Rendering;

class WaylandFramebuffer(IWaylandFramebufferSurface surface) : IFramebufferPlatformSurface, IPlatformRenderSurface
{
    private readonly IWaylandFramebufferSurface _surface = surface;
    public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new RenderTarget(this);


    class RenderTarget : IFramebufferRenderTarget, IPlatformRenderSurface
    {
        private readonly WaylandFramebuffer _p;
        // Buffers are reused once the compositor releases them.
        private readonly List<ShmBuffer> _buffers = new();
        private readonly List<PixelRect> _frameDirtyRects = new();
        private PixelSize _size;
        private PixelSize _presentedSize;
        private bool _damageAll = true;
        private bool _disposed;

        public RenderTarget(WaylandFramebuffer p)
        {
            _p = p;
            p._surface.RegisterRenderTarget(this);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _p._surface.UnregisterRenderTarget(this);
            // The compositor keeps its own mapping of the pool, so busy buffers can be destroyed too.
            foreach (var buffer in _buffers)
                buffer.Dispose();
            _buffers.Clear();
        }

        public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties)
        {
            if (!State.IsReady)
                throw new RenderTargetNotReadyException();

            var size = sceneInfo.Size;
            size = new PixelSize(Math.Max(1, size.Width), Math.Max(1, size.Height));

            var buffer = AcquireBuffer(size);
            var wlBuffer = buffer.WlBuffer!;
            var session = SupportsPartialPresentation ? new PartialPresentationSession(this, buffer.Contents) : null;

            properties = new FramebufferLockProperties { PartialPresentation = session };
            return new LockedFramebuffer(buffer.Address, size, buffer.Stride,
                new Vector(96 * sceneInfo.Scaling, 96 * sceneInfo.Scaling),
                PixelFormats.Bgra8888, AlphaFormat.Premul, () =>
                {
                    if (session is { IsSucceeded: false })
                    {
                        buffer.InvalidateContents();
                        _damageAll = true;
                        return;
                    }

                    if (!State.IsReady)
                    {
                        // The compositor still shows an older frame.
                        _damageAll = true;
                        return;
                    }

                    // Stage per-frame state (frame callback +
                    // ack_configure + geometry + viewport/scale +
                    // min/max) into the next commit BEFORE binding the
                    // buffer, then commit explicitly after attach +
                    // damage. The base WSurface no longer issues the
                    // commit itself.
                    _p._surface.OnBeforeNewBufferAttached(sceneInfo);
                    _p._surface.WlSurface!.Attach(wlBuffer, 0, 0);
                    if (session is { HasDirtyRects: true } && !_damageAll && _presentedSize == size)
                    {
                        foreach (var rect in _frameDirtyRects)
                        {
                            var damage = rect.Intersect(new PixelRect(size));
                            if (damage.Width > 0 && damage.Height > 0)
                                _p._surface.WlSurface.DamageBuffer(damage.X, damage.Y, damage.Width, damage.Height);
                        }
                    }
                    else
                        _p._surface.WlSurface.DamageBuffer(0, 0, size.Width, size.Height);
                    _damageAll = false;
                    _presentedSize = size;
                    buffer.Busy = true;
                    _p._surface.WlSurface.Commit();
                });
        }

        private ShmBuffer AcquireBuffer(PixelSize size)
        {
            _size = size;
            ShmBuffer? free = null;
            for (var i = _buffers.Count - 1; i >= 0; i--)
            {
                var buffer = _buffers[i];
                if (buffer.Size != size)
                {
                    // Busy buffers of an old size are destroyed when the compositor releases them.
                    if (!buffer.Busy)
                    {
                        buffer.Dispose();
                        _buffers.RemoveAt(i);
                    }
                }
                else if (!buffer.Busy)
                    free = buffer;
            }

            if (free != null)
                return free;

            var created = ShmBuffer.Create(_p._surface.Globals!.WlShm, size, OnBufferReleased);
            _buffers.Add(created);

            // Surfaces that allocate buffers outside the throttled render loop must
            // flush the buffer's fd immediately: libwayland-client caps the number of
            // fds per wl_display_flush (~28, undocumented), and a roundtrip flushes.
            if (_p._surface.EnforceBufferCreationRoundtrip)
                _p._surface.Globals!.Connection.Queue.Roundtrip();

            return created;
        }

        public void SetFrameDirtyRects(ReadOnlySpan<PixelRect> rects)
        {
            _frameDirtyRects.Clear();
            foreach (var rect in rects)
                _frameDirtyRects.Add(rect);
        }

        private void OnBufferReleased(ShmBuffer buffer)
        {
            buffer.Busy = false;
            if (_disposed)
                return;
            if (buffer.Size != _size && _buffers.Remove(buffer))
                buffer.Dispose();
        }

        public bool RetainsFrameContents => false;

        public bool SupportsPartialPresentation => _p._surface.Globals?.UsePartialPresentation == true;

        public PlatformRenderTargetState State =>
            _disposed ? PlatformRenderTargetState.Disposed : _p._surface.State;
    }

    sealed class PartialPresentationSession(RenderTarget target, IPartialPresentationBuffer buffer)
        : IPartialPresentationFeature
    {
        public IPartialPresentationBuffer Buffer => buffer;
        public bool HasDirtyRects { get; private set; }
        public bool IsSucceeded { get; private set; }

        public void SetDirtyRects(ReadOnlySpan<PixelRect> frameDirtyRects, ReadOnlySpan<PixelRect> bufferDirtyRects)
        {
            if (HasDirtyRects)
                throw new InvalidOperationException("Dirty rectangles can only be declared once per frame.");
            HasDirtyRects = true;
            target.SetFrameDirtyRects(frameDirtyRects);
        }

        public void Succeeded() => IsSucceeded = true;
    }

    // Identifies the contents of a buffer; replaced when the contents become unknown.
    sealed class BufferContents : IPartialPresentationBuffer
    {
        public bool IsValid { get; set; } = true;
        public bool Equals(IPartialPresentationBuffer? other) => ReferenceEquals(this, other);
    }

    class ShmBuffer : IDisposable
    {
        private readonly int _length;

        private ShmBuffer(IntPtr address, int length, PixelSize size, int stride)
        {
            Address = address;
            _length = length;
            Size = size;
            Stride = stride;
        }

        public IntPtr Address { get; private set; }
        public PixelSize Size { get; }
        public int Stride { get; }
        public WlBuffer? WlBuffer { get; private set; }
        public bool Busy { get; set; }
        public BufferContents Contents { get; private set; } = new();

        public void InvalidateContents()
        {
            Contents.IsValid = false;
            Contents = new BufferContents();
        }

        public static ShmBuffer Create(WlShm shm, PixelSize size, Action<ShmBuffer> onReleased)
        {
            var stride = size.Width * 4;
            var length = stride * size.Height;

            var fd = memfd_create("avalonia-wayland-framebuffer", MFD_CLOEXEC);
            try
            {
                IntPtr map;
                if (fd == -1
                    || ftruncate(fd, length) != 0
                    || new IntPtr(-1) == (map =
                        mmap(IntPtr.Zero, length, PROT_READ | PROT_WRITE, MAP_SHARED, fd, IntPtr.Zero)))
                {
                    throw new OutOfMemoryException("Unable to allocate framebuffer");
                }

                var buffer = new ShmBuffer(map, length, size, stride);
                try
                {
                    var pool = shm.CreatePool(fd, length);
                    try
                    {
                        buffer.WlBuffer = pool.CreateBuffer(0, size.Width, size.Height, stride,
                            WlShm.FormatEnum.Argb8888, new BufferListener(buffer, onReleased));
                    }
                    finally
                    {
                        // NWayland's Dispose only destroys the local proxy;
                        // the generated method is required to marshal the
                        // Wayland destructor request to the compositor.
                        pool.Destroy();
                    }
                }
                catch
                {
                    buffer.Dispose();
                    throw;
                }

                return buffer;
            }
            finally
            {
                // libwayland-client duplicates the fd when marshalling the request.
                if (fd != -1)
                    close(fd);
            }
        }

        public void Dispose()
        {
            Contents.IsValid = false;
            if (WlBuffer != null)
            {
                WlBuffer.Destroy();
                WlBuffer.Dispose();
                WlBuffer = null;
            }

            if (Address != IntPtr.Zero)
            {
                munmap(Address, _length);
                Address = IntPtr.Zero;
            }
        }

        private class BufferListener(ShmBuffer buffer, Action<ShmBuffer> onReleased) : WlBuffer.Listener
        {
            protected override void Release(WlBuffer eventSender) => onReleased(buffer);
        }
    }


    public bool IsReady => _surface.State.IsReady;
}
