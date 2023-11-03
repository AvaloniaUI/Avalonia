using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media;
using HarfBuzzSharp;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class GlyphTypefaceImpl : IGlyphTypeface, IGlyphTypeface2
    {
        private bool _isDisposed;
        private readonly SKTypeface _typeface;

        public GlyphTypefaceImpl(SKTypeface typeface, FontSimulations fontSimulations)
        {
            _typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));

            Face = new Face(GetTable)
            {
                UnitsPerEm = typeface.UnitsPerEm
            };

            Font = new Font(Face);

            Font.SetFunctionsOpenType();

            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.HorizontalAscender, out var ascent);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.HorizontalDescender, out var descent);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.HorizontalLineGap, out var lineGap);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.StrikeoutOffset, out var strikethroughOffset);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.StrikeoutSize, out var strikethroughSize);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.UnderlineOffset, out var underlineOffset);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.UnderlineSize, out var underlineSize);

            Metrics = new FontMetrics
            {
                DesignEmHeight = (short)Face.UnitsPerEm,
                Ascent = -ascent,
                Descent = -descent,
                LineGap = lineGap,
                UnderlinePosition = -underlineOffset,
                UnderlineThickness = underlineSize,
                StrikethroughPosition = -strikethroughOffset,
                StrikethroughThickness = strikethroughSize,
                IsFixedPitch = typeface.IsFixedPitch
            };

            GlyphCount = typeface.GlyphCount;

            FontSimulations = fontSimulations;

            Weight = (FontWeight)typeface.FontWeight;

            Style = typeface.FontSlant.ToAvalonia();

            Stretch = (FontStretch)typeface.FontStyle.Width;
        }

        public Face Face { get; }

        public Font Font { get; }

        public FontSimulations FontSimulations { get; }

        public int ReplacementCodepoint { get; }

        public FontMetrics Metrics { get; }

        public int GlyphCount { get; }

        public string FamilyName => _typeface.FamilyName;

        public FontWeight Weight { get; }

        public FontStyle Style { get; }

        public FontStretch Stretch { get; }

        public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics)
        {
            metrics = default;

            if (!Font.TryGetGlyphExtents(glyph, out var extents))
            {
                return false;
            }

            metrics = new GlyphMetrics
            {
                XBearing = extents.XBearing,
                YBearing = extents.YBearing,
                Width = extents.Width,
                Height = extents.Height
            };

            return true;
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public ushort GetGlyph(uint codepoint)
        {
            if (Font.TryGetGlyph(codepoint, out var glyph))
            {
                return (ushort)glyph;
            }

            return 0;
        }

        public bool TryGetGlyph(uint codepoint, out ushort glyph)
        {
            glyph = GetGlyph(codepoint);

            return glyph != 0;
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
        {
            var glyphs = new ushort[codepoints.Length];

            for (var i = 0; i < codepoints.Length; i++)
            {
                if (Font.TryGetGlyph(codepoints[i], out var glyph))
                {
                    glyphs[i] = (ushort)glyph;
                }
            }

            return glyphs;
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public int GetGlyphAdvance(ushort glyph)
        {
            return Font.GetHorizontalGlyphAdvance(glyph);
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            var glyphIndices = new uint[glyphs.Length];

            for (var i = 0; i < glyphs.Length; i++)
            {
                glyphIndices[i] = glyphs[i];
            }

            return Font.GetHorizontalGlyphAdvances(glyphIndices);
        }

        private Blob? GetTable(Face face, Tag tag)
        {
            var size = _typeface.GetTableSize(tag);

            var data = Marshal.AllocCoTaskMem(size);

            var releaseDelegate = new ReleaseDelegate(() => Marshal.FreeCoTaskMem(data));

            return _typeface.TryGetTableData(tag, 0, size, data) ?
                new Blob(data, size, MemoryMode.ReadOnly, releaseDelegate) : null;
        }

        public SKFont CreateSKFont(float size)
            => new(_typeface, size, skewX: (FontSimulations & FontSimulations.Oblique) != 0 ? -0.3f : 0.0f)
            {
                LinearMetrics = true,
                Embolden = (FontSimulations & FontSimulations.Bold) != 0
            };

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (!disposing)
            {
                return;
            }

            Font.Dispose();
            Face.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool TryGetTable(uint tag, out byte[] table)
        {
            return _typeface.TryGetTableData(tag, out table);
        }

        public bool TryGetStream([NotNullWhen(true)] out Stream? stream)
        {
            try
            {
                var asset = _typeface.OpenStream();
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
    }
}
