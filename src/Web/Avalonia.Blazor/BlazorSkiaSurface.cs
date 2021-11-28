using Avalonia;
using Avalonia.Blazor.Interop;
using SkiaSharp;

namespace Avalonia.Blazor;

internal class BlazorSkiaSurface
{
    public SKColorType ColorType { get; set; }
        
    public PixelSize Size { get; set; }

    public GRContext Context { get; set; }
        
    public GRSurfaceOrigin Origin { get; set; }

    public double Scaling { get; set; }

    public SKHtmlCanvasInterop.GLInfo GlInfo { get; set; }
}