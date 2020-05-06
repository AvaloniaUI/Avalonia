using System;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <inheritdoc />
    public class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl(SKTextBlob textBlob)
        {
            TextBlob = textBlob;
        }

        /// <summary>
        ///     Gets the text blob to draw.
        /// </summary>
        public SKTextBlob TextBlob { get; }

        void IDisposable.Dispose()
        {
            TextBlob.Dispose();
        }
    }
}
