using System;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Metadata;
using HarfBuzzSharp;
using SkiaSharp;

namespace Avalonia.Skia
{
    [Unstable]
    public class GlyphTypefaceImpl : IGlyphTypeface
    {
        private bool _isDisposed;

        public GlyphTypefaceImpl(SKTypeface typeface, bool isFakeBold = false, bool isFakeItalic = false)
        {
            Typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));

            Face = new Face(GetTable)
            {
                UnitsPerEm = Typeface.UnitsPerEm
            };

            Font = new Font(Face);

            Font.SetFunctionsOpenType();

            var metrics = Typeface.ToFont().Metrics;

            const double defaultFontRenderingEmSize = 12.0;

            Metrics = new FontMetrics
            {
                DesignEmHeight = (short)Typeface.UnitsPerEm,
                Ascent = (int)(metrics.Ascent / defaultFontRenderingEmSize * Typeface.UnitsPerEm),
                Descent = (int)(metrics.Descent / defaultFontRenderingEmSize * Typeface.UnitsPerEm),
                LineGap = (int)(metrics.Leading / defaultFontRenderingEmSize * Typeface.UnitsPerEm),
                UnderlinePosition = metrics.UnderlinePosition != null ?
                (int)(metrics.UnderlinePosition / defaultFontRenderingEmSize * Typeface.UnitsPerEm) :
                0,
                UnderlineThickness = metrics.UnderlineThickness != null ?
                (int)(metrics.UnderlineThickness / defaultFontRenderingEmSize * Typeface.UnitsPerEm) :
                0,
                StrikethroughPosition = metrics.StrikeoutPosition != null ?
                (int)(metrics.StrikeoutPosition / defaultFontRenderingEmSize * Typeface.UnitsPerEm) :
                0,
                StrikethroughThickness = metrics.StrikeoutThickness != null ?
                (int)(metrics.StrikeoutThickness / defaultFontRenderingEmSize * Typeface.UnitsPerEm) :
                0,
                IsFixedPitch = Typeface.IsFixedPitch
            };

            GlyphCount = Typeface.GlyphCount;

            IsFakeBold = isFakeBold;

            IsFakeItalic = isFakeItalic;
        }

        public Face Face { get; }

        public Font Font { get; }

        public SKTypeface Typeface { get; }

        public int ReplacementCodepoint { get; }

        public FontMetrics Metrics { get; }

        public int GlyphCount { get; }

        public bool IsFakeBold { get; }

        public bool IsFakeItalic { get; }

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

        private Blob GetTable(Face face, Tag tag)
        {
            var size = Typeface.GetTableSize(tag);

            var data = Marshal.AllocCoTaskMem(size);

            var releaseDelegate = new ReleaseDelegate(() => Marshal.FreeCoTaskMem(data));

            return Typeface.TryGetTableData(tag, 0, size, data) ?
                new Blob(data, size, MemoryMode.ReadOnly, releaseDelegate) : null;
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
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool TryGetTable(uint tag, out byte[] table)
        {
            return Typeface.TryGetTableData(tag, out table);
        }
    }
}
