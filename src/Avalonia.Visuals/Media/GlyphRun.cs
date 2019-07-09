// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Media
{
    public class GlyphRun
    {
        private IGlyphRunImpl _glyphRunImpl;

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
        }

        public GlyphTypeface GlyphTypeface { get; }

        public float FontRenderingEmSize { get; }

        public Point BaselineOrigin { get; }

        public ushort[] GlyphIndices { get; }

        public float[] GlyphAdvances { get; }

        public Vector[] GlyphOffsets { get; }

        public Rect Bounds => GlyphRunImpl.Bounds;

        public IGlyphRunImpl GlyphRunImpl =>
            _glyphRunImpl ?? (_glyphRunImpl = GlyphTypeface.GlyphTypefaceImpl.CreateGlyphRun(
                FontRenderingEmSize, BaselineOrigin,
                GlyphIndices, GlyphAdvances, GlyphOffsets));
    }
}
