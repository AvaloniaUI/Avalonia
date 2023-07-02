using System;
using Avalonia.Collections.Pooled;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Framebuffer
{
    internal class WlFramebufferSurface : IFramebufferRenderTarget
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly WlWindow _wlWindow;
        private readonly PooledStack<ResizableBuffer> _buffers;

        public WlFramebufferSurface(AvaloniaWaylandPlatform platform, WlWindow wlWindow)
        {
            _platform = platform;
            _wlWindow = wlWindow;
            _buffers = new PooledStack<ResizableBuffer>();
        }

        public ILockedFramebuffer Lock()
        {
            var width = (int)Math.Round(_wlWindow.AppliedState.Size.Width * _wlWindow.RenderScaling, MidpointRounding.AwayFromZero);
            var height = (int)Math.Round(_wlWindow.AppliedState.Size.Height * _wlWindow.RenderScaling, MidpointRounding.AwayFromZero);
            var stride = width * 4;

            if (!_buffers.TryPop(out var resizableBuffer))
                resizableBuffer = new ResizableBuffer(_platform, x => _buffers.Push(x));

            return resizableBuffer.GetFramebuffer(_wlWindow.WlSurface, width, height, stride, _wlWindow.RenderScaling);
        }

        public void Dispose()
        {
            foreach (var buffer in _buffers)
                buffer.Dispose();
            _buffers.Dispose();
        }

        private sealed class ResizableBuffer : WlBuffer.IEvents, IDisposable
        {
            private readonly AvaloniaWaylandPlatform _platform;
            private readonly Action<ResizableBuffer> _onRelease;

            private int _size;
            private IntPtr _data;
            private WlBuffer? _wlBuffer;

            public ResizableBuffer(AvaloniaWaylandPlatform platform, Action<ResizableBuffer> onRelease)
            {
                _platform = platform;
                _onRelease = onRelease;
            }

            public WlFramebuffer GetFramebuffer(WlSurface wlSurface, int width, int height, int stride, double scale)
            {
                var size = stride * height;

                if (_size != size)
                {
                    _wlBuffer?.Dispose();
                    _wlBuffer = null;
                    LibC.munmap(_data, _size);
                    _data = IntPtr.Zero;
                }

                if (_wlBuffer is null)
                {
                    var fd = FdHelper.CreateAnonymousFile(size, "wayland-shm");
                    if (fd == -1)
                        throw new WaylandPlatformException("Failed to create FrameBuffer");
                    _data = LibC.mmap(IntPtr.Zero, size, MemoryProtection.PROT_READ | MemoryProtection.PROT_WRITE, SharingType.MAP_SHARED, fd, 0);
                    using var wlShmPool = _platform.WlShm.CreatePool(fd, size);
                    _wlBuffer = wlShmPool.CreateBuffer(0, width, height, stride, WlShm.FormatEnum.Argb8888);
                    _wlBuffer.Events = this;
                    _size = size;
                    LibC.close(fd);
                }

                return new WlFramebuffer(wlSurface, _wlBuffer, _data, new PixelSize(width, height), stride, scale);
            }

            public void OnRelease(WlBuffer eventSender) => _onRelease.Invoke(this);

            public void Dispose()
            {
                _wlBuffer?.Dispose();
                if (_data != IntPtr.Zero)
                    LibC.munmap(_data, _size);
            }
        }
    }
}
