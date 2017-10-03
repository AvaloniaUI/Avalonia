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
            // This method may be called from non-UI thread, don't touch anything that calls back to GTK/GDK
            var s = _window.ClientSize;
            var width = (int) s.Width;
            var height = (int) s.Height;
            return new ImageSurfaceFramebuffer(_window, width, height, _window.LastKnownScaleFactor);
        }
    }
}
