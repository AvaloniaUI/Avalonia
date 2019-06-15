using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia
{
    public class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl(SKPoint[] glyphPositions, Rect bounds)
        {
            GlyphPositions = glyphPositions;
            Bounds = bounds;
        }
        public SKPoint[] GlyphPositions { get; }
        public Rect Bounds { get; }
    }
}
