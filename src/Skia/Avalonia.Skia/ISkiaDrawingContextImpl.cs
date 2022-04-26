using Avalonia.Metadata;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    [Unstable]
    public interface ISkiaDrawingContextImpl : IDrawingContextImpl
    {
        SKCanvas SkCanvas { get; }
        GRContext GrContext { get; }
        SKSurface SkSurface { get; }
    }
}
