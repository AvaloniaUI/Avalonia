﻿using System;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Platform;
using HarfBuzzSharp;
using SkiaSharp;

namespace Avalonia.Skia
{
    public class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        private bool _isDisposed;

        public GlyphTypefaceImpl(SKTypeface typeface)
        {
            Typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));

            Face = new Face(GetTable)
            {
                UnitsPerEm = Typeface.UnitsPerEm
            };

            Font = new Font(Face);

            Font.SetFunctionsOpenType();

            Font.GetScale(out var xScale, out _);

            DesignEmHeight = (short)xScale;

            if (!Font.TryGetHorizontalFontExtents(out var fontExtents))
            {
                Font.TryGetVerticalFontExtents(out fontExtents);
            }

            Ascent = -fontExtents.Ascender;

            Descent = -fontExtents.Descender;

            LineGap = fontExtents.LineGap;

            if (Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.UnderlineOffset, out var underlinePosition))
            {
                UnderlinePosition = underlinePosition;
            }

            if (Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.UnderlineSize, out var underlineThickness))
            {
                UnderlineThickness = underlineThickness;
            }

            if (Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.StrikeoutOffset, out var strikethroughPosition))
            {
                StrikethroughPosition = strikethroughPosition;
            }

            if (Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.StrikeoutSize, out var strikethroughThickness))
            {
                StrikethroughThickness = strikethroughThickness;
            }

            IsFixedPitch = Typeface.IsFixedPitch;
        }

        public Face Face { get; }

        public Font Font { get; }

        public SKTypeface Typeface { get; }

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
    }
}
