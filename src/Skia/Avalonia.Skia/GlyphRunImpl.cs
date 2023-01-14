using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Platform;
using SkiaSharp;
#nullable enable

namespace Avalonia.Skia
{
    /// <inheritdoc />
    [Unstable]
    public class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl(SKTextBlob textBlob)
        {
            TextBlob = textBlob ?? throw new ArgumentNullException (nameof (textBlob));
        }

        /// <summary>
        ///     Gets the text blob to draw.
        /// </summary>
        public SKTextBlob TextBlob { get; }

        public IReadOnlyList<float> GetIntersections(float upperBound, float lowerBound) => 
            TextBlob.GetIntercepts(lowerBound, upperBound);

        void IDisposable.Dispose()
        {
            TextBlob.Dispose();
        }
    }
}
