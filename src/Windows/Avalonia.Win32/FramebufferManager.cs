using System;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class FramebufferManager : IFramebufferPlatformSurface, IDisposable
    {
        private readonly IntPtr _hwnd;
        private WindowFramebuffer _fb;

        public FramebufferManager(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        public ILockedFramebuffer Lock()
        {
            UnmanagedMethods.GetClientRect(_hwnd, out var rc);

            var width = Math.Max(1, rc.right - rc.left);
            var height = Math.Max(1, rc.bottom - rc.top);

            if ((_fb == null || _fb.Size.Width != width || _fb.Size.Height != height))
            {
                _fb?.Deallocate();
                _fb = null;
                _fb = new WindowFramebuffer(_hwnd, new PixelSize(width, height));
            }

            return _fb;
        }

        public void Dispose()
        {
            _fb?.Deallocate();
            _fb = null;
        }
    }
}
