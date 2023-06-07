using Avalonia.Browser.Interop;
using SkiaSharp;

namespace Avalonia.Browser.Skia
{
    internal class BrowserSkiaSurface : IBrowserSkiaSurface
    {
        public BrowserSkiaSurface(GRContext context, GLInfo glInfo, SKColorType colorType, PixelSize size, double scaling, GRSurfaceOrigin origin)
        {
            Context = context;
            GlInfo = glInfo;
            ColorType = colorType;
            Size = size;
            Scaling = scaling;
            Origin = origin;
        }

        public SKColorType ColorType { get; set; }

        public PixelSize Size { get; set; }

        public GRContext Context { get; set; }

        public GRSurfaceOrigin Origin { get; set; }

        public double Scaling { get; set; }

        public GLInfo GlInfo { get; set; }
    }
}
