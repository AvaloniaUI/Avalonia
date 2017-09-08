using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Cairo;
using Gdk;

namespace Avalonia.Gtk
{
    class SurfaceFramebuffer : ILockedFramebuffer
    {
        private Drawable _drawable;
        private ImageSurface _surface;

        public SurfaceFramebuffer(int width, int height)
        {
            _surface = new ImageSurface(Cairo.Format.RGB24, width, height);
        }

        public void SetDrawable(Drawable drawable)
        {
            _drawable = drawable;
            _surface.Flush();
        }

        public void Deallocate()
        {
            _surface.Dispose();
            _surface = null;
        }

        public void Dispose()
        {
            using (var ctx = CairoHelper.Create(_drawable))
            {
                _surface.MarkDirty();
                ctx.SetSourceSurface(_surface, 0, 0);
                ctx.Paint();
            }
            _drawable = null;
        }

        public IntPtr Address => _surface.DataPtr;
        public int Width => _surface.Width;
        public int Height => _surface.Height;
        public int RowBytes => _surface.Stride;
        //TODO: Proper DPI detect
        public Vector Dpi => new Vector(96, 96);
        public PixelFormat Format => PixelFormat.Bgra8888;
    }
}

