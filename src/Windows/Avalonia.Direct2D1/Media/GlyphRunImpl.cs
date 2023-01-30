using System.Collections.Generic;
using Avalonia.Platform;
using SharpDX.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    internal class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl(GlyphRun glyphRun, Size size, Point baselineOrigin)
        {
            Size = size;
            BaselineOrigin = baselineOrigin;
            GlyphRun = glyphRun;
        }

        public Size Size { get; }

        public Point BaselineOrigin { get; }

        public GlyphRun GlyphRun { get; }

        public void Dispose()
        {
            //GlyphRun?.Dispose();
        }

        public IReadOnlyList<float> GetIntersections(float lowerBound, float upperBound)
        {
            return null;
        }
    }
}
