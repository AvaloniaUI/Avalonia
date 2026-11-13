using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Variation;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class SkiaTypeface : IPlatformTypeface, IPlatformTypefaceSimulator
    {
        private readonly FontSimulations _platformSimulations;
        private readonly bool _ownsTypeface;

        public SkiaTypeface(SKTypeface typeface, FontSimulations fontSimulations)
            : this(typeface ?? throw new ArgumentNullException(nameof(typeface)), fontSimulations,
                FontSimulations.None, typeface.FontStyle.Slant.ToAvalonia(), (FontWeight)typeface.FontWeight,
                (FontStretch)typeface.FontWidth)
        {
        }

        /// <summary>
        /// Creates a typeface that reports the given style, weight and stretch.
        /// </summary>
        /// <param name="typeface">The Skia typeface.</param>
        /// <param name="fontSimulations">The simulations applied when the typeface is drawn.</param>
        /// <param name="platformSimulations">
        /// The simulations the platform already applied inside <paramref name="typeface"/>. They are
        /// reported in <see cref="FontSimulations"/> but never applied a second time.
        /// </param>
        /// <param name="style">The reported style.</param>
        /// <param name="weight">The reported weight.</param>
        /// <param name="stretch">The reported stretch.</param>
        /// <param name="ownsTypeface">
        /// Whether disposing this instance disposes <paramref name="typeface"/>. A simulated variant
        /// shares the typeface of its source and leaves it to the source.
        /// </param>
        public SkiaTypeface(SKTypeface typeface, FontSimulations fontSimulations, FontSimulations platformSimulations,
            FontStyle style, FontWeight weight, FontStretch stretch, bool ownsTypeface = true)
        {
            SKTypeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
            FontSimulations = fontSimulations | platformSimulations;
            _platformSimulations = platformSimulations;
            _ownsTypeface = ownsTypeface;
            Weight = weight;
            Style = style;
            Stretch = stretch;
        }

        public SKTypeface SKTypeface { get; }

        public FontSimulations FontSimulations { get; }

        public string FamilyName => SKTypeface.FamilyName;

        public FontWeight Weight { get; }

        public FontStyle Style { get; }

        public FontStretch Stretch { get; }

        /// <summary>
        /// Gets the simulations the platform applied inside <see cref="SKTypeface"/>.
        /// </summary>
        internal FontSimulations PlatformSimulations => _platformSimulations;

        public SKFont CreateSKFont(float size)
        {
            var appliedSimulations = FontSimulations & ~_platformSimulations;

            return new(SKTypeface, size, skewX: (appliedSimulations & FontSimulations.Oblique) != 0 ? -0.3f : 0.0f)
            {
                LinearMetrics = true,
                Embolden = (appliedSimulations & FontSimulations.Bold) != 0
            };
        }

        public bool TryCreateSimulatedTypeface(FontSimulations fontSimulations, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? typeface)
        {
            // The platform's own simulations are part of the Skia typeface, so a variant without one
            // of them cannot be drawn from it.
            if ((_platformSimulations & ~fontSimulations) != 0)
            {
                typeface = null;

                return false;
            }

            typeface = new SkiaTypeface(SKTypeface, fontSimulations & ~_platformSimulations, _platformSimulations,
                style, weight, stretch, ownsTypeface: false);

            return true;
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
                // SKStream.Read doesn't keep the stream alive across its native call in SkiaSharp 3.x. Without
                // the using, the finalizer can destroy the native stream while it's still being read.
                using var asset = SKTypeface.OpenStream();
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

        /// <summary>
        /// Gets whether a typeface has a colour glyph table: COLR layers, sbix or CBDT bitmaps, or SVG
        /// documents. Only the table directory is read, since a bitmap table of a colour emoji font is
        /// large.
        /// </summary>
        /// <remarks>
        /// A colour font face is simulated like any other face, since most such faces also have outline
        /// glyphs. Whether a simulation reaches its colour glyphs is up to the renderer that draws them.
        /// </remarks>
        internal static bool IsColorFont(SKTypeface typeface)
        {
            ReadTableDirectory(typeface, out var isColorFont, out _);

            return isColorFont;
        }

        /// <summary>
        /// Reads the table directory of a typeface once for whether it is a colour font face, as
        /// <see cref="IsColorFont"/> decides it, and whether it has an fvar table.
        /// </summary>
        internal static void ReadTableDirectory(SKTypeface typeface, out bool isColorFont, out bool hasFvar)
        {
            isColorFont = false;
            hasFvar = false;

            foreach (var tag in typeface.GetTableTags())
            {
                if (ColorGlyphTables.Contains(tag))
                {
                    isColorFont = true;
                }
                else if (tag == FvarTable.Tag)
                {
                    hasFvar = true;
                }
            }
        }

        public void Dispose()
        {
            if (_ownsTypeface)
            {
                SKTypeface.Dispose();
            }
        }
    }
}
