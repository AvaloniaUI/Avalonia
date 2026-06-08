using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Wayland.Server.Persistent;
using NWayland.Protocols.Wayland;
using static Avalonia.Wayland.Server.Interop.UnsafeNativeMethods;
namespace Avalonia.Wayland.Server.Transient.Rendering;

class WaylandFramebuffer(WSurface surface) : IFramebufferPlatformSurface, IPlatformRenderSurface
{
    private readonly WSurface _surface = surface;
    public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new RenderTarget(this);


    class RenderTarget : IFramebufferRenderTarget, IPlatformRenderSurface
    {
        private readonly WaylandFramebuffer _p;
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
        }

        class BufferListener : WlBuffer.Listener
        {
            protected override void Release(WlBuffer eventSender)
            {
                // TODO: Pool buffers
                eventSender.Destroy();
                base.Release(eventSender);
            }
        }
        

        public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties)
        {
            if (!State.IsReady)
                throw new RenderTargetNotReadyException();

            var size = sceneInfo.Size;
            var bufferLen = sceneInfo.Size.Width * sceneInfo.Size.Height * 4;
            var stride = size.Width * 4;
            
            var fd = memfd_create("avalonia-wayland-framebuffer", 1);
            IntPtr map;
            if (fd == -1
                || ftruncate(fd, bufferLen) != 0
                || new IntPtr(-1) == (map =
                    mmap(IntPtr.Zero, bufferLen, 3, 1, fd, IntPtr.Zero)))
            {
                close(fd);
                throw new OutOfMemoryException("Unable to allocate framebuffer");
            }

            properties = default;
            return new LockedFramebuffer(map, size, stride,
                new Vector(96 * sceneInfo.Scaling, 96 * sceneInfo.Scaling),
                PixelFormats.Bgra8888, AlphaFormat.Premul, () =>
                {
                    munmap(map, bufferLen);
                    try
                    {
                        if (!State.IsReady)
                            return;

                        using var pool = _p._surface.Globals!.WlShm.CreatePool(fd, bufferLen);
                        var listener = new BufferListener();
                        var buffer = pool.CreateBuffer(0, size.Width, size.Height, stride,
                            WlShm.FormatEnum.Argb8888, listener);
                        
                        // Stage per-frame state (frame callback +
                        // ack_configure + geometry + viewport/scale +
                        // min/max) into the next commit BEFORE binding the
                        // buffer, then commit explicitly after attach +
                        // damage. The base WSurface no longer issues the
                        // commit itself.
                        _p._surface.OnBeforeNewBufferAttached(sceneInfo);
                        _p._surface.WlSurface!.Attach(buffer, 0, 0);
                        // TODO: Support "damage" regions
                        _p._surface.WlSurface.DamageBuffer(0, 0, size.Width, size.Height);
                        _p._surface.WlSurface.Commit();
                    }
                    finally
                    {
                        close(fd);
                    }
                });
        }

        public bool RetainsFrameContents => false;

        public PlatformRenderTargetState State =>
            _disposed ? PlatformRenderTargetState.Disposed : _p._surface.State;
    }


    public bool IsReady => _surface.State.IsReady;
}
