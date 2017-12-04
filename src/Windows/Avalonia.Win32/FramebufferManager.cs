using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_hwnd, out rc);
            var width = rc.right - rc.left;
            var height = rc.bottom - rc.top;
            if ((_fb == null || _fb.Width != width || _fb.Height != height) && width > 0 && height > 0)
            {
                _fb?.Deallocate();
                _fb = null;
                _fb = new WindowFramebuffer(_hwnd, width, height);
            }
            return _fb;
        }

        public void Dispose()
        {
            _fb?.Deallocate();
        }
    }
}
