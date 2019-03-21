// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

using Avalonia.Media;

using HarfBuzzSharp;

using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Avalonia.Skia
{
    internal class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        private readonly Face _face;
        private readonly Font _font;

        public GlyphTypefaceImpl(SKTypeface typeface)
        {
            Typeface = typeface;

            using (var blob = typeface.OpenStream(out var index).ToHarfBuzzBlob())
            {
                _face = new Face(blob, index);
            }

            _face.MakeImmutable();

            _font = new Font(_face);            
        }

        public SKTypeface Typeface { get; }

        public int Ascent => -_font.HorizontalFontExtents.Ascender;

        public int Descent => -_font.HorizontalFontExtents.Descender;

        public int LineGap => _font.HorizontalFontExtents.LineGap;

        public int UnderlinePosition => 0;

        public int UnderlineThickness => 0;

        public int StrikethroughPosition => 0;

        public int StrikethroughThickness => 0;

        public ReadOnlySpan<short> GetGlyphs(ReadOnlySpan<int> text)
        {
            var glyphs = new short[text.Length];

            for (var i = 0; i < text.Length; i++)
            {
                glyphs[i] = (short)_font.GetGlyph(text[i]);
            }

            return glyphs;
        }

        public ReadOnlySpan<int> GetGlyphAdvances(ReadOnlySpan<short> glyphs)
        {
            var indices = new int[glyphs.Length];

            for (var i = 0; i < glyphs.Length; i++)
            {
                indices[i] = glyphs[i];
            }

            return _font.GetHorizontalGlyphAdvances(indices);
        }

        public void Dispose()
        {
            _font.Dispose();
            _face.Dispose();
        }
    }
}
