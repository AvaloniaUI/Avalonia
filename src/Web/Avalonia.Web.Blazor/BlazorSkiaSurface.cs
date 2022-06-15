using Avalonia.Web.Blazor.Interop;
using SkiaSharp;

namespace Avalonia.Web.Blazor
{
    internal class BlazorSkiaSurface : IBlazorSkiaSurface
    {
        public BlazorSkiaSurface(GRContext context, SKHtmlCanvasInterop.GLInfo glInfo, SKColorType colorType, PixelSize size, double scaling, GRSurfaceOrigin origin)
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

        public SKHtmlCanvasInterop.GLInfo GlInfo { get; set; }
    }
}
