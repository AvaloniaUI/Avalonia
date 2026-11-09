using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class SkiaTypeface : IPlatformTypeface
    {
        public SkiaTypeface(SKTypeface typeface, FontSimulations fontSimulations)
        {
            SKTypeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
            FontSimulations = fontSimulations;
            Weight = (FontWeight)typeface.FontWeight;
            Style = typeface.FontStyle.Slant.ToAvalonia();
            Stretch = (FontStretch)typeface.FontWidth;
        }

        /// <summary>
        /// Creates a typeface for a face the platform matched to a request. When the match needs
        /// simulations it stands for the requested face, so it reports the requested style, weight
        /// and stretch instead of the design values of the matched face. A font collection then
        /// recognizes it as the face it asked for instead of synthesizing that face again.
        /// </summary>
        public SkiaTypeface(SKTypeface typeface, FontSimulations fontSimulations, FontStyle style, FontWeight weight,
            FontStretch stretch) : this(typeface, fontSimulations)
        {
            if (fontSimulations != FontSimulations.None)
            {
                Style = style;
                Weight = weight;
                Stretch = stretch;
            }
        }

        public SKTypeface SKTypeface { get; }

        public FontSimulations FontSimulations { get; }

        public string FamilyName => SKTypeface.FamilyName;

        public FontWeight Weight { get; }

        public FontStyle Style { get; }

        public FontStretch Stretch { get; }

        public SKFont CreateSKFont(float size)
        {
            return new(SKTypeface, size, skewX: (FontSimulations & FontSimulations.Oblique) != 0 ? -0.3f : 0.0f)
            {
                LinearMetrics = true,
                Embolden = (FontSimulations & FontSimulations.Bold) != 0
            };
        }

        public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table)
        {
            table = default;

            if (SKTypeface.TryGetTableData(tag, out var data))
            {
                table = data;

                return true;
            }

            return false;
        }

        public bool TryGetStream([NotNullWhen(true)] out Stream? stream)
        {
            try
            {
                var asset = SKTypeface.OpenStream();
                var size = asset.Length;
                var buffer = new byte[size];

                asset.Read(buffer, size);

                stream = new MemoryStream(buffer);

                return true;
            }
            catch
            {
                stream = null;

                return false;
            }
        }

        public void Dispose()
        {
            SKTypeface.Dispose();
        }
    }
}
