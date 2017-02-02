using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Platform.Surfaces;

namespace Avalonia.Gtk3
{
    class FramebufferManager : IFramebufferPlatformSurface, IDisposable
    {
        private readonly TopLevelImpl _window;
        private ImageSurfaceFramebuffer _fb;
        public FramebufferManager(TopLevelImpl window)
        {
            _window = window;
        }

        public void Dispose()
        {
            _fb?.Deallocate();
        }

        public ILockedFramebuffer Lock()
        {
            if(_window.CurrentCairoContext == IntPtr.Zero)
                throw new InvalidOperationException("Window is not in drawing state");

            var ctx = _window.CurrentCairoContext;
            var width = (int) _window.ClientSize.Width;
            var height = (int) _window.ClientSize.Height;
            if (_fb == null || _fb.Width != width ||
                _fb.Height != height)
            {
                _fb?.Dispose();
                _fb = new ImageSurfaceFramebuffer(width, height);
            }
            _fb.Prepare(ctx);
            return _fb;
        }
    }
}
