// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

using SkiaSharp;

namespace Avalonia.Skia.Text
{
    public class SKGlyphRun
    {
        public SKGlyphRun(ushort[] glyphIndices, SKPoint[] glyphOffsets, IReadOnlyList<SKGlyphCluster> glyphClusters)
        {
            GlyphIndices = glyphIndices;
            GlyphOffsets = glyphOffsets;
            GlyphClusters = glyphClusters;
        }

        /// <summary>
        /// Gets an array of <see cref="ushort"/> values that represent the glyph indices in the underlying font.
        /// </summary>
        /// <value>
        /// The glyph ids.
        /// </value>
        public ushort[] GlyphIndices { get; }

        /// <summary>
        /// Gets an array of <see cref="SKPoint"/> values representing the offsets of the glyphs in the <see cref="SKGlyphRun"/>.
        /// </summary>
        /// <value>
        /// The glyph positions.
        /// </value>
        public SKPoint[] GlyphOffsets { get; }

        /// <summary>
        /// Gets the glyph clusters.
        /// </summary>
        /// <value>
        /// The glyph clusters.
        /// </value>
        public IReadOnlyList<SKGlyphCluster> GlyphClusters { get; }
    }
}
