using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;


namespace Avalonia.Gtk3
{
    class ImageSurfaceFramebuffer : ILockedFramebuffer
    {
        private IntPtr _context;
        private readonly GtkWidget _widget;
        private CairoSurface _surface;
        private int _factor;

        public ImageSurfaceFramebuffer(IntPtr context, GtkWidget widget, int width, int height)
        {
            _context = context;
            _widget = widget;
            _factor = (int)(Native.GtkWidgetGetScaleFactor?.Invoke(_widget) ?? 1u);
            width *= _factor;
            height *= _factor;
            _surface = Native.CairoImageSurfaceCreate(1, width, height);
            
            Width = width;
            Height = height;
            Address = Native.CairoImageSurfaceGetData(_surface);
            RowBytes = Native.CairoImageSurfaceGetStride(_surface);
            Native.CairoSurfaceFlush(_surface);
        }
        
        public void Dispose()
        {
            if(_context == IntPtr.Zero || _surface == null)
                return;
            Native.CairoSurfaceMarkDirty(_surface);
            Native.CairoScale(_context, 1d / _factor, 1d / _factor);
            Native.CairoSetSourceSurface(_context, _surface, 0, 0);
            Native.CairoPaint(_context);
            _context = IntPtr.Zero;
            _surface.Dispose();
            _surface = null;
        }

        public IntPtr Address { get; }
        public int Width  { get; }
        public int Height { get; }
        public int RowBytes { get; }

        
        public Vector Dpi
        {
            get
            {
                return new Vector(96, 96) * _factor;
            }
        }

        public PixelFormat Format => PixelFormat.Bgra8888;
    }
}



