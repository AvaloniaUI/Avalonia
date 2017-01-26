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
        private IntPtr _surface;

        public ImageSurfaceFramebuffer(int width, int height)
        {
            _surface = Native.CairoImageSurfaceCreate(1, width, height);
            Width = width;
            Height = height;
            Address = Native.CairoImageSurfaceGetData(_surface);
            RowBytes = Native.CairoImageSurfaceGetStride(_surface);
            Native.CairoSurfaceFlush(_surface);
        }

        public void Prepare(IntPtr context)
        {
            _context = context;
        }

        public void Deallocate()
        {
            Native.CairoSurfaceDestroy(_surface);
            _surface = IntPtr.Zero;
        }

        public void Dispose()
        {
            if(_context == IntPtr.Zero || _surface == IntPtr.Zero)
                return;
            Native.CairoSurfaceMarkDirty(_surface);
            Native.CairoSetSourceSurface(_context, _surface, 0, 0);
            Native.CairoPaint(_context);
            _context = IntPtr.Zero;

        }

        public IntPtr Address { get; }
        public int Width  { get; }
        public int Height { get; }
        public int RowBytes { get; }

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



