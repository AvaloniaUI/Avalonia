using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;
using Avalonia.Threading;


namespace Avalonia.Gtk3
{
    class ImageSurfaceFramebuffer : ILockedFramebuffer
    {
        private readonly WindowBaseImpl _impl;
        private readonly GtkWidget _widget;
        private CairoSurface _surface;
        private int _factor;
        private object _lock = new object();
        public ImageSurfaceFramebuffer(WindowBaseImpl impl, int width, int height)
        {
            _impl = impl;
            _widget = impl.GtkWidget;
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

        static void Draw(IntPtr context, CairoSurface surface, double factor)
        {
            
            Native.CairoSurfaceMarkDirty(surface);
            Native.CairoScale(context, 1d / factor, 1d / factor);
            Native.CairoSetSourceSurface(context, surface, 0, 0);
            Native.CairoPaint(context);

        }
        /*
        static Stopwatch St =Stopwatch.StartNew();
        private static int _frames;
        private static int _fps;*/
        static void DrawToWidget(GtkWidget widget, CairoSurface surface, int width, int height, double factor)
        {
            if(surface == null || widget.IsClosed)
                return;
            var window = Native.GtkWidgetGetWindow(widget);
            if(window == IntPtr.Zero)
                return;
            var rc = new GdkRectangle {Width = width, Height = height};
            Native.GdkWindowBeginPaintRect(window, ref rc);
            var context = Native.GdkCairoCreate(window);
            Draw(context, surface, factor);
            /*
            _frames++;
            var el = St.Elapsed;
            if (el.TotalSeconds > 1)
            {
                _fps = (int) (_frames / el.TotalSeconds);
                _frames = 0;
                St = Stopwatch.StartNew();
            }
            
            Native.CairoSetSourceRgba(context, 1, 0, 0, 1);
            Native.CairoMoveTo(context, 20, 20);
            Native.CairoSetFontSize(context, 30);
            using (var txt = new Utf8Buffer("FPS: " + _fps))
                Native.CairoShowText(context, txt);
            */
            
            Native.CairoDestroy(context);
            Native.GdkWindowEndPaint(window);
        }
        
        class RenderOp : IDeferredRenderOperation
        {
            private readonly GtkWidget _widget;
            private CairoSurface _surface;
            private readonly double _factor;
            private readonly int _width;
            private readonly int _height;

            public RenderOp(GtkWidget widget, CairoSurface _surface, double factor, int width, int height)
            {
                _widget = widget;
                this._surface = _surface;
                _factor = factor;
                _width = width;
                _height = height;
            }

            public void Dispose()
            {
                _surface?.Dispose();
                _surface = null;
            }

            public void RenderNow()
            {
                DrawToWidget(_widget, _surface, _width, _height, _factor);
            }
        }
        
        public void Dispose()
        {
            lock (_lock)
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    if (_impl.CurrentCairoContext != IntPtr.Zero)
                        Draw(_impl.CurrentCairoContext, _surface, _factor);
                    else
                        DrawToWidget(_widget, _surface, Width, Height, _factor);
                    _surface.Dispose();
                }
                else
                    _impl.SetNextRenderOperation(new RenderOp(_widget, _surface, _factor, Width, Height));
                _surface = null;
            }
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



