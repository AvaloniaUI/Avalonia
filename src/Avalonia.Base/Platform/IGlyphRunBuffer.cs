using System;
using System.Drawing;

namespace Avalonia.Platform
{
    public interface IGlyphRunBuffer
    {
        Span<ushort> GlyphIndices { get; }

        IGlyphRunImpl Build();
    }

    public interface IHorizontalGlyphRunBuffer : IGlyphRunBuffer
    {
        Span<float> GlyphPositions { get; }
    }

    public interface IPositionedGlyphRunBuffer : IGlyphRunBuffer
    {
        Span<PointF> GlyphPositions { get; }
    }
}
