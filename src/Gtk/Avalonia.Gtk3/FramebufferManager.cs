using System;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Gtk3
{
    class FramebufferManager : IFramebufferPlatformSurface, IDisposable
    {
        private readonly WindowBaseImpl _window;
        public FramebufferManager(WindowBaseImpl window)
        {
            _window = window;
        }

        public void Dispose()
        {
            //
        }

        public ILockedFramebuffer Lock()
        {
            // This method may be called from non-UI thread, don't touch anything that calls back to GTK/GDK
            var s = _window.ClientSize;
            var width = Math.Max(1, (int) s.Width);
            var height = Math.Max(1, (int) s.Height);
            
            
            if (!Dispatcher.UIThread.CheckAccess() && Gtk3Platform.DisplayClassName.ToLower().Contains("x11"))
            {
                var x11 = LockX11Framebuffer(width, height);
                if (x11 != null)
                    return x11;
            }
            

            return new ImageSurfaceFramebuffer(_window, width, height, _window.LastKnownScaleFactor);
        }

        private static int X11ErrorHandler(IntPtr d, ref X11.XErrorEvent e)
        {
            return 0;
        }

        private static X11.XErrorHandler X11ErrorHandlerDelegate = X11ErrorHandler;
        
        private static IntPtr X11Display;
        private ILockedFramebuffer LockX11Framebuffer(int width, int height)
        {
            if (!_window.GdkWindowHandle.HasValue)
                return null;
            if (X11Display == IntPtr.Zero)
            {
                X11Display = X11.XOpenDisplay(IntPtr.Zero);
                if (X11Display == IntPtr.Zero)
                    return null;
                X11.XSetErrorHandler(X11ErrorHandlerDelegate);
            }
            return new X11Framebuffer(X11Display, _window.GdkWindowHandle.Value, width, height, _window.LastKnownScaleFactor);
        }
    }
}
