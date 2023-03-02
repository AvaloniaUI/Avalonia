using System;
using System.Collections.Generic;
using Avalonia.Platform;
using SharpDX.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    internal class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl(GlyphRun glyphRun, Size size, Point baselineOrigin)
        {
            Bounds = new Rect(new Point(baselineOrigin.X, 0), size);
            BaselineOrigin = baselineOrigin;
            GlyphRun = glyphRun;
        }

        public Rect Bounds{ get; }

        public Point BaselineOrigin { get; }

        public GlyphRun GlyphRun { get; }

        public void Dispose()
        {
            //GlyphRun?.Dispose();
        }

        public IReadOnlyList<float> GetIntersections(float lowerBound, float upperBound)
            => Array.Empty<float>();
    }
}
