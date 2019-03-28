// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;

namespace Avalonia.Direct2D1.Media
{
    using System;

    using SharpDX.DirectWrite;

    public class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        private readonly FontFace _fontFace;

        public GlyphTypefaceImpl(Typeface typeface)
        {
            // ToDo: remove font size parameter
            var textFormat = Direct2D1FontCollectionCache.GetTextFormat(typeface, 12);

            var fontCollection = textFormat.FontCollection;

            fontCollection.FindFamilyName(typeface.FontFamily.Name, out var index);

            Font = fontCollection.GetFontFamily(index).GetFirstMatchingFont(
                (FontWeight)typeface.Weight,
                FontStretch.Normal,
                (FontStyle)typeface.Style);

            var fontMetrics = Font.Metrics;

            _fontFace = new FontFace(Font);

            DesignEmHeight = fontMetrics.DesignUnitsPerEm;

            Ascent = -fontMetrics.Ascent;

            Descent = fontMetrics.Descent;

            LineGap = fontMetrics.LineGap;

            UnderlinePosition = fontMetrics.UnderlinePosition;

            UnderlineThickness = fontMetrics.UnderlineThickness;

            StrikethroughPosition = fontMetrics.StrikethroughPosition;

            StrikethroughThickness = fontMetrics.StrikethroughThickness;
        }

        public Font Font { get; }

        public short DesignEmHeight { get; }

        public int Ascent { get; }

        public int Descent { get; }

        public int LineGap { get; }

        public int UnderlinePosition { get; }

        public int UnderlineThickness { get; }

        public int StrikethroughPosition { get; }

        public int StrikethroughThickness { get; }

        public void Dispose()
        {
            _fontFace.Dispose();
        }

        public short[] GetGlyphs(ReadOnlySpan<char> text)
        {
            var codePoints = new int[text.Length];

            for (var i = 0; i < text.Length; i++)
            {
                codePoints[i] = text[i];
            }

            return GetGlyphs(codePoints);
        }

        public short[] GetGlyphs(int[] codePoints)
        {
            return _fontFace.GetGlyphIndices(codePoints);
        }

        public ReadOnlySpan<int> GetGlyphAdvances(short[] glyphs)
        {
            var glyphMetrics = _fontFace.GetDesignGlyphMetrics(glyphs, false);

            var glyphAdvances = new int[glyphMetrics.Length];

            for (var i = 0; i < glyphMetrics.Length; i++)
            {
                glyphAdvances[i] = glyphMetrics[i].AdvanceWidth;
            }

            return glyphAdvances;
        }
    }
}
