using System;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class FramebufferManager : IFramebufferPlatformSurface
    {
        private readonly IntPtr _hwnd;
        private WindowFramebuffer _fb;

        public FramebufferManager(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        public ILockedFramebuffer Lock()
        {
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_hwnd, out rc);
            var width = rc.right - rc.left;
            var height = rc.bottom - rc.top;
            if ((_fb == null || _fb.Size.Width != width || _fb.Size.Height != height) && width > 0 && height > 0)
            {
                _fb?.Deallocate();
                _fb = null;
                _fb = new WindowFramebuffer(_hwnd, new PixelSize(width, height));
            }
            return _fb;
        }
    }
}
