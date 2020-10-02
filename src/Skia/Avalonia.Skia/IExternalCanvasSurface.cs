using SkiaSharp;

namespace Avalonia.Skia
{
    public interface IExternalCanvasSurface
    {
        SKCanvas Canvas { get; }

        Vector Dpi { get; }
    }
}
