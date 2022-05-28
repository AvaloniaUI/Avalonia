using System;
using Avalonia.Metadata;
using Avalonia.Platform;
using JetBrains.Annotations;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <inheritdoc />
    [Unstable]
    public class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl([NotNull] SKTextBlob textBlob)
        {
            TextBlob = textBlob ?? throw new ArgumentNullException (nameof (textBlob));
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
