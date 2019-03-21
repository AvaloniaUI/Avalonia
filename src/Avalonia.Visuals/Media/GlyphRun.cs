// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Media
{
    public class GlyphRun
    {
        public GlyphRun(
            GlyphTypeface glyphTypeface,            
            double renderingEmSize,
            Point baselineOrigin,
            IReadOnlyList<short> glyphIndices,
            IReadOnlyList<double> glyphAdvances,
            IReadOnlyList<Vector> glyphOffsets)
        {
            GlyphTypeface = glyphTypeface;
            RenderingEmSize = renderingEmSize;
            GlyphIndices = glyphIndices;
            BaselineOrigin = baselineOrigin;
            GlyphAdvances = glyphAdvances;
            GlyphOffsets = glyphOffsets;
            Size = GetSize();
        }

        public GlyphTypeface GlyphTypeface { get; }

        public double RenderingEmSize { get; }

        public Point BaselineOrigin { get; }

        public IReadOnlyList<short> GlyphIndices { get; }

        public IReadOnlyList<double> GlyphAdvances { get; }

        public IReadOnlyList<Vector> GlyphOffsets { get; }

        public Size Size { get; }

        private Size GetSize()
        {
            var width = 0.0d;

            foreach (var glyphAdvance in GlyphAdvances)
            {
                width += glyphAdvance;
            }

            return new Size(width, (GlyphTypeface.Descent - GlyphTypeface.Ascent) * RenderingEmSize);
        }
    }
}
