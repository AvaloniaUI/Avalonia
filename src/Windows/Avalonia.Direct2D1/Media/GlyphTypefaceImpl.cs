// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

using Avalonia.Media;

namespace Avalonia.Direct2D1.Media
{
    using SharpDX.DirectWrite;

    public class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        public GlyphTypefaceImpl(Typeface typeface)
        {
            var textFormat = Direct2D1FontCollectionCache.GetTextFormat(typeface, 12);

            var fontCollection = textFormat.FontCollection;

            fontCollection.FindFamilyName(typeface.FontFamily.Name, out var index);

            var font = fontCollection.GetFontFamily(index).GetFirstMatchingFont(
                (FontWeight)typeface.Weight,
                FontStretch.Normal,
                (FontStyle)typeface.Style);

            var fontMetrics = font.Metrics;

            FontFace = new FontFace(font);
            Ascent = fontMetrics.Ascent;
            Descent = fontMetrics.Descent;
            LineGap = fontMetrics.LineGap;
            UnderlinePosition = fontMetrics.UnderlinePosition;
            UnderlineThickness = fontMetrics.UnderlineThickness;
            StrikethroughPosition = fontMetrics.StrikethroughPosition;
            StrikethroughThickness = fontMetrics.StrikethroughThickness;
        }

        public FontFace FontFace { get; }

        public int Ascent { get; }

        public int Descent { get; }

        public int LineGap { get; }

        public int UnderlinePosition { get; }

        public int UnderlineThickness { get; }

        public int StrikethroughPosition { get; }

        public int StrikethroughThickness { get; }

        public void Dispose()
        {
            FontFace.Dispose();
        }

        public ReadOnlySpan<short> GetGlyphs(ReadOnlySpan<int> text)
        {            
            return FontFace.GetGlyphIndices(text.ToArray());
        }

        public ReadOnlySpan<int> GetGlyphAdvances(ReadOnlySpan<short> glyphs)
        {                     
            var glyphMetrics = FontFace.GetDesignGlyphMetrics(glyphs.ToArray(), false);

            var glyphAdvances = new int[glyphMetrics.Length];

            for (var i = 0; i < glyphMetrics.Length; i++)
            {
                glyphAdvances[i] = glyphMetrics[i].AdvanceWidth;
            }

            return glyphAdvances;
        }
    }
}
