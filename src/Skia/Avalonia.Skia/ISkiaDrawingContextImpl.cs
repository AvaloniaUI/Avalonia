using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    public interface ISkiaDrawingContextImpl : IDrawingContextImpl
    {
        SKCanvas SkCanvas { get; }
        GRContext GrContext { get; }
        SKSurface SkSurface { get; }
        double CurrentOpacity { get; }
    }
}
