// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Media
{
    // ToDo: Introduce GlyphRunImpl for better performance (caching)
    public class GlyphRun
    {
        public GlyphRun(
            GlyphTypeface glyphTypeface,
            float fontRenderingEmSize,
            Point baselineOrigin,
            ushort[] glyphIndices,
            float[] glyphAdvances = null,
            Vector[] glyphOffsets = null)
        {
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            GlyphIndices = glyphIndices;
            BaselineOrigin = baselineOrigin;
            GlyphAdvances = glyphAdvances;
            GlyphOffsets = glyphOffsets;
            Size = GetSize();
        }

        public GlyphTypeface GlyphTypeface { get; }

        public float FontRenderingEmSize { get; }

        public Point BaselineOrigin { get; }

        public ushort[] GlyphIndices { get; }

        public float[] GlyphAdvances { get; }

        public Vector[] GlyphOffsets { get; }

        public Size Size { get; }

        private Size GetSize()
        {
            var scale = FontRenderingEmSize / GlyphTypeface.DesignEmHeight;

            var width = 0.0d;

            if (GlyphAdvances != null)
            {
                foreach (var glyphAdvance in GlyphAdvances)
                {
                    width += glyphAdvance;
                }
            }
            else
            {
                var glyphAdvances = GlyphTypeface.GetGlyphAdvances(GlyphIndices);

                foreach (var advance in glyphAdvances)
                {
                    width += advance * scale;
                }
            }

            return new Size(width, (GlyphTypeface.Descent - GlyphTypeface.Ascent + GlyphTypeface.LineGap) * scale);
        }
    }
}
