using System;
using System.IO;
using Avalonia.Media;
using HarfBuzzSharp;

namespace Avalonia.UnitTests
{
    public class HarfBuzzGlyphTypefaceImpl : IGlyphTypeface
    {
        private bool _isDisposed;
        private Blob _blob;

        public HarfBuzzGlyphTypefaceImpl(Stream data)
        {
            _blob = Blob.FromStream(data);
            
            Face = new Face(_blob, 0);

            Font = new Font(Face);

            Font.SetFunctionsOpenType();

            Font.GetScale(out var scale, out _);

            const double defaultFontRenderingEmSize = 12.0;

            var metrics = Font.OpenTypeMetrics;

            Metrics = new FontMetrics
            {
                DesignEmHeight = (short)scale,
                Ascent = (int)(metrics.GetXVariation(OpenTypeMetricsTag.HorizontalAscender) / defaultFontRenderingEmSize * scale),
                Descent = (int)(metrics.GetXVariation(OpenTypeMetricsTag.HorizontalDescender) / defaultFontRenderingEmSize * scale),
                LineGap = (int)(metrics.GetXVariation(OpenTypeMetricsTag.HorizontalLineGap) / defaultFontRenderingEmSize * scale),

                UnderlinePosition = (int)(metrics.GetXVariation(OpenTypeMetricsTag.UnderlineOffset) / defaultFontRenderingEmSize * scale),

                UnderlineThickness = (int)(metrics.GetXVariation(OpenTypeMetricsTag.UnderlineSize) / defaultFontRenderingEmSize * scale),

                StrikethroughPosition = (int)(metrics.GetXVariation(OpenTypeMetricsTag.StrikeoutOffset) / defaultFontRenderingEmSize * scale),

                StrikethroughThickness = (int)(metrics.GetXVariation(OpenTypeMetricsTag.StrikeoutSize) / defaultFontRenderingEmSize * scale),

                IsFixedPitch = GetGlyphAdvance(GetGlyph('a')) == GetGlyphAdvance(GetGlyph('b'))
            };           

            GlyphCount = Face.GlyphCount;
        }

        public FontMetrics Metrics { get; }

        public Face Face { get; }

        public Font Font { get; }

        public int GlyphCount { get; set; }

        public FontSimulations FontSimulations { get; }

        public string FamilyName => "$Default";

        public FontWeight Weight { get; }

        public FontStyle Style { get; }

        public FontStretch Stretch { get; }


        /// <inheritdoc cref="IGlyphTypeface"/>
        public ushort GetGlyph(uint codepoint)
        {
            if (Font.TryGetGlyph(codepoint, out var glyph))
            {
                return (ushort)glyph;
            }

            return 0;
        }

        public bool TryGetGlyph(uint codepoint,out ushort glyph)
        {
            glyph = 0;

            if (Font.TryGetGlyph(codepoint, out var glyphId))
            {
                glyph = (ushort)glyphId;

                return true;
            }

            return false;
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

        public bool TryGetTable(uint tag, out byte[] table)
        {
            table = null;
            var blob = Face.ReferenceTable(tag);

            if (blob.Length > 0)
            {
                table = blob.AsSpan().ToArray();

                return true;
            }

            return false;
        }

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

            Font?.Dispose();
            Face?.Dispose();
            _blob?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
    }
}
