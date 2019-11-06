// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Platform;
using HarfBuzzSharp;
using SharpDX.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    public class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        private bool _isDisposed;

        public GlyphTypefaceImpl(Typeface typeface)
        {
            DWFont = Direct2D1FontCollectionCache.GetFont(typeface);

            FontFace = new FontFace(DWFont);

            Face = new Face(GetTable);

            Font = new HarfBuzzSharp.Font(Face);

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
        }

        private Blob GetTable(Face face, Tag tag)
        {
            var dwTag = (int)SwapBytes(tag);

            if (FontFace.TryGetFontTable(dwTag, out var tableData, out _))
            {
                return new Blob(tableData.Pointer, tableData.Size, MemoryMode.ReadOnly, () => { });
            }

            return null;
        }

        private static uint SwapBytes(uint x)
        {
            x = (x >> 16) | (x << 16);

            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        public SharpDX.DirectWrite.Font DWFont { get; }

        public FontFace FontFace { get; }

        public Face Face { get; }

        public HarfBuzzSharp.Font Font { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public short DesignEmHeight { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int Ascent { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int Descent { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int LineGap { get; }

        //ToDo: Read font table for these values
        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int UnderlinePosition { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int UnderlineThickness { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int StrikethroughPosition { get; }

        /// <inheritdoc cref="IGlyphTypefaceImpl"/>
        public int StrikethroughThickness { get; }

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
            FontFace?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

