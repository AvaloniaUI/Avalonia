using System;
using System.IO;
using Avalonia.Platform;
using HarfBuzzSharp;

namespace Avalonia.UnitTests
{
   public class HarfBuzzGlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        private bool _isDisposed;
        private Blob _blob;

        public HarfBuzzGlyphTypefaceImpl(Stream data, bool isFakeBold = false, bool isFakeItalic = false)
        {
            _blob = Blob.FromStream(data);
            
            Face = new Face(_blob, 0);

            Font = new Font(Face);

            Font.SetFunctionsOpenType();

            Font.GetScale(out var scale, out _);
            
            DesignEmHeight = (short)scale;

            var metrics = Font.OpenTypeMetrics;

            const double defaultFontRenderingEmSize = 12.0;

            Ascent = (int)(metrics.GetXVariation(OpenTypeMetricsTag.HorizontalAscender) / defaultFontRenderingEmSize * DesignEmHeight);

            Descent = (int)(metrics.GetXVariation(OpenTypeMetricsTag.HorizontalDescender) / defaultFontRenderingEmSize * DesignEmHeight);

            LineGap = (int)(metrics.GetXVariation(OpenTypeMetricsTag.HorizontalLineGap) / defaultFontRenderingEmSize * DesignEmHeight);

            UnderlinePosition = (int)(metrics.GetXVariation(OpenTypeMetricsTag.UnderlineOffset) / defaultFontRenderingEmSize * DesignEmHeight);

            UnderlineThickness = (int)(metrics.GetXVariation(OpenTypeMetricsTag.UnderlineSize) / defaultFontRenderingEmSize * DesignEmHeight);

            StrikethroughPosition = (int)(metrics.GetXVariation(OpenTypeMetricsTag.StrikeoutOffset) / defaultFontRenderingEmSize * DesignEmHeight);

            StrikethroughThickness = (int)(metrics.GetXVariation(OpenTypeMetricsTag.StrikeoutSize) / defaultFontRenderingEmSize * DesignEmHeight);

            IsFixedPitch = GetGlyphAdvance(GetGlyph('a')) == GetGlyphAdvance(GetGlyph('b'));

            GlyphCount = Face.GlyphCount;

            IsFakeBold = isFakeBold;

            IsFakeItalic = isFakeItalic;
        }

        public Face Face { get; }

        public Font Font { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public short DesignEmHeight { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int Ascent { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int Descent { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int LineGap { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int UnderlinePosition { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int UnderlineThickness { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int StrikethroughPosition { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int StrikethroughThickness { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public bool IsFixedPitch { get; }

        public int GlyphCount { get; set; }
        
        public bool IsFakeBold { get; }
        
        public bool IsFakeItalic { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public ushort GetGlyph(uint codepoint)
        {
            if (Font.TryGetGlyph(codepoint, out var glyph))
            {
                return (ushort)glyph;
            }

            return 0;
        }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
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

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int GetGlyphAdvance(ushort glyph)
        {
            return Font.GetHorizontalGlyphAdvance(glyph);
        }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            var glyphIndices = new uint[glyphs.Length];

            for (var i = 0; i < glyphs.Length; i++)
            {
                glyphIndices[i] = glyphs[i];
            }

            return Font.GetHorizontalGlyphAdvances(glyphIndices);
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
    }
}
