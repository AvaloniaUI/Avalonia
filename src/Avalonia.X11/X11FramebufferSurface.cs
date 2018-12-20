using System;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    public class X11FramebufferSurface : IFramebufferPlatformSurface
    {
        private readonly IntPtr _display;
        private readonly IntPtr _xid;

        public X11FramebufferSurface(IntPtr display, IntPtr xid)
        {
            _display = display;
            _xid = xid;
        }
        
        public ILockedFramebuffer Lock()
        {
            XLockDisplay(_display);
            XGetGeometry(_display, _xid, out var root, out var x, out var y, out var width, out var height,
                out var bw, out var d);
            XUnlockDisplay(_display);
            return new X11Framebuffer(_display, _xid, width, height, 1);
        }
    }
}
