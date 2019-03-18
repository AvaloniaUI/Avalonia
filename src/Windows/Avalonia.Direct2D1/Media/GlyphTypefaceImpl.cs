// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
            Ascent = (double)fontMetrics.Ascent / fontMetrics.DesignUnitsPerEm;
            Descent = (double)fontMetrics.Descent / fontMetrics.DesignUnitsPerEm;
            Leading = (double)fontMetrics.LineGap / fontMetrics.DesignUnitsPerEm;
            UnderlinePosition = (double)fontMetrics.UnderlinePosition / fontMetrics.DesignUnitsPerEm;
            UnderlineThickness = (double)fontMetrics.UnderlineThickness / fontMetrics.DesignUnitsPerEm;
            StrikethroughPosition = (double)fontMetrics.StrikethroughPosition / fontMetrics.DesignUnitsPerEm;
            StrikethroughThickness = (double)fontMetrics.StrikethroughThickness / fontMetrics.DesignUnitsPerEm;
        }

        public FontFace FontFace { get; }

        public double Ascent { get; }

        public double Descent { get; }

        public double Leading { get; }

        public double UnderlinePosition { get; }

        public double UnderlineThickness { get; }

        public double StrikethroughPosition { get; }

        public double StrikethroughThickness { get; }

        public ushort CharacterToGlyph(char c)
        {
            return (ushort)FontFace.GetGlyphIndices(new int[] { c })[0];
        }

        public ushort CharacterToGlyph(int c)
        {
            return (ushort)FontFace.GetGlyphIndices(new[] { c })[0];
        }

        public double GetHorizontalGlyphAdvance(ushort glyph)
        {
            var glyphMetrics = FontFace.GetDesignGlyphMetrics(new[] { (short)glyph }, false)[0];

            return glyphMetrics.AdvanceWidth;
        }

        public void Dispose()
        {
            FontFace.Dispose();
        }
    }
}
