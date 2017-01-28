using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Gdk;

namespace Avalonia.Gtk
{
    class PixbufFramebuffer : ILockedFramebuffer
    {
        private Pixbuf _pixbuf;
        private Drawable _drawable;

        public PixbufFramebuffer(int width, int height)
        {
            _pixbuf = new Pixbuf(Gdk.Colorspace.Rgb, false, 8, width, height);
        }

        public void SetDrawable(Drawable drawable)
        {
            _drawable = drawable;
        }

        public void Deallocate()
        {
            _pixbuf.Dispose();
            _pixbuf = null;
        }

        public void Dispose()
        {
            using (var gc = new Gdk.GC(_drawable))
                _drawable.DrawPixbuf(gc, _pixbuf, 0, 0, 0, 0, Width, Height, RgbDither.None, 0, 0);
            _drawable = null;
        }

        public IntPtr Address => _pixbuf.Pixels;
        public int Width => _pixbuf.Width;
        public int Height => _pixbuf.Height;
        public int RowBytes => _pixbuf.Rowstride;
        //TODO: Proper DPI detect
        public Size Dpi => new Size(96, 96);
        public PixelFormat Format
        {
            get
            {
                if (AvaloniaLocator.Current.GetService<IRuntimePlatform>().GetRuntimeInfo().OperatingSystem ==
                    OperatingSystemType.WinNT)
                    return PixelFormat.Bgra8888;
                return PixelFormat.Rgba8888;
            }
        }
    }
}

