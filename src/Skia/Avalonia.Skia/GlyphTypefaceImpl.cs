// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;

using HarfBuzzSharp;

using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Avalonia.Skia
{
    internal class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        private static SKPaint s_paint = new SKPaint { TextSize = 12 };

        private readonly Face _face;
        private readonly Font _font;
        private readonly double _fontScale;
        private readonly SKFontMetrics _fontMetrics;

        public GlyphTypefaceImpl(SKTypeface typeface)
        {
            Typeface = typeface;

            using (var blob = typeface.OpenStream(out var index).ToHarfBuzzBlob())
            {
                _face = new Face(blob, index);
            }

            _face.MakeImmutable();

            _font = new Font(_face);

            _fontScale = 12d / _font.Scale.X;

            _fontMetrics = GetFontMetrics(typeface);
        }

        public SKTypeface Typeface { get; }

        public double Ascent => _fontMetrics.Ascent;

        public double Descent => _fontMetrics.Descent;

        public double Leading => _fontMetrics.Leading;

        public double UnderlinePosition => _fontMetrics.UnderlinePosition ?? _fontMetrics.Descent;

        public double UnderlineThickness => _fontMetrics.UnderlineThickness ?? 1.0d;

        public double StrikethroughPosition => _fontMetrics.StrikeoutPosition ?? 0;

        public double StrikethroughThickness => _fontMetrics.StrikeoutThickness ?? 1.0d;

        public ushort CharacterToGlyph(char c)
        {
            return (ushort)_font.GetGlyph(c);
        }

        public ushort CharacterToGlyph(int c)
        {
            return (ushort)_font.GetGlyph(c);
        }

        public double GetHorizontalGlyphAdvance(ushort glyph)
        {
            return _fontScale * _font.GetHorizontalGlyphAdvance(glyph);
        }

        public void Dispose()
        {
            _font.Dispose();
            _face.Dispose();
        }

        private static SKFontMetrics GetFontMetrics(SKTypeface typeface)
        {
            s_paint.Typeface = typeface;

            return s_paint.FontMetrics;
        }
    }
}
