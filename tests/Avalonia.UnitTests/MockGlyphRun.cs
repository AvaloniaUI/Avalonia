using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockGlyphRun : IGlyphRunImpl
    {
        public MockGlyphRun(IGlyphTypeface glyphTypeface, double fontRenderingEmSize, Point baselineOrigin, Rect bounds)
        {
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BaselineOrigin = baselineOrigin;
            Bounds =bounds;
        }

        public IGlyphTypeface GlyphTypeface { get; }

        public double FontRenderingEmSize { get; }

        public Point BaselineOrigin { get; }

        public Rect Bounds { get; }

        public void Dispose()
        {
           
        }

        public IReadOnlyList<float> GetIntersections(float lowerLimit, float upperLimit) 
            => Array.Empty<float>();
    }
}
