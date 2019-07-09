// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Skia.Text;

using SkiaSharp;

namespace Avalonia.Skia
{
    internal class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        private readonly TableLoader _tableLoader;

        public GlyphTypefaceImpl(SKTypeface typeface)
        {
            Typeface = typeface;

            _tableLoader = TableLoader.Get(typeface);

            _tableLoader.Font.GetScale(out var xScale, out _);

            DesignEmHeight = (short)xScale;

            if (!_tableLoader.Font.TryGetHorizontalFontExtents(out var fontExtents) &&
                !_tableLoader.Font.TryGetVerticalFontExtents(out fontExtents))
            {
                return;
            }

            Ascent = -fontExtents.Ascender;

            Descent = -fontExtents.Descender;

            LineGap = fontExtents.LineGap;
        }

        public SKTypeface Typeface { get; }

        public short DesignEmHeight { get; }

        public int Ascent { get; }

        public int Descent { get; }

        public int LineGap { get; }

        public int UnderlinePosition => 0;

        public int UnderlineThickness => 0;

        public int StrikethroughPosition => 0;

        public int StrikethroughThickness => 0;

        public ushort[] GetGlyphs(ReadOnlySpan<uint> codePoints)
        {
            var glyphs = new ushort[codePoints.Length];

            for (var i = 0; i < codePoints.Length; i++)
            {
                if (_tableLoader.Font.TryGetGlyph(codePoints[i], out var glyph))
                {
                    glyphs[i] = (ushort)glyph;
                }
            }

            return glyphs;
        }

        public ReadOnlySpan<int> GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            var glyphIndices = new uint[glyphs.Length];

            for (var i = 0; i < glyphs.Length; i++)
            {
                glyphIndices[i] = glyphs[i];
            }

            return _tableLoader.Font.GetHorizontalGlyphAdvances(glyphIndices);
        }
    }
}
