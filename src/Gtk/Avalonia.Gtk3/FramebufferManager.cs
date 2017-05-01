using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

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
            if(_window.CurrentCairoContext == IntPtr.Zero)
                throw new InvalidOperationException("Window is not in drawing state");
            var width = (int) _window.ClientSize.Width;
            var height = (int) _window.ClientSize.Height;
            return new ImageSurfaceFramebuffer(_window.CurrentCairoContext, _window.GtkWidget, width, height);
        }
    }
}
