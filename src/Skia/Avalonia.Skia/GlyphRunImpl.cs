using System;
using System.Collections.Generic;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl(SKTextBlob textBlob, Size size, Point baselineOrigin)
        {
            TextBlob = textBlob ?? throw new ArgumentNullException(nameof(textBlob));

            Bounds = new Rect(new Point(baselineOrigin.X, 0), size);

            BaselineOrigin = baselineOrigin;
        }

        /// <summary>
        ///     Gets the text blob to draw.
        /// </summary>
        public SKTextBlob TextBlob { get; }

        public Rect Bounds { get; }

        public Point BaselineOrigin { get; }

        public IReadOnlyList<float> GetIntersections(float upperBound, float lowerBound) => 
            TextBlob.GetIntercepts(lowerBound, upperBound);

        void IDisposable.Dispose()
        {
            TextBlob.Dispose();
        }
    }
}
