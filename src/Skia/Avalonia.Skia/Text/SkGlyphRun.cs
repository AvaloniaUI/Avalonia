// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

using SkiaSharp;

namespace Avalonia.Skia.Text
{  
    public class SKGlyphRun
    {
        public SKGlyphRun(byte[] glyphIds, SKPoint[] glyphPositions, IReadOnlyList<SKGlyphCluster> glyphClusters)
        {
            GlyphIds = glyphIds;
            GlyphPositions = glyphPositions;
            GlyphClusters = glyphClusters;
        }

        /// <summary>
        /// Gets the glyph ids.
        /// </summary>
        /// <value>
        /// The glyph ids.
        /// </value>
        public byte[] GlyphIds { get; }

        /// <summary>
        /// Gets the glyph positions.
        /// </summary>
        /// <value>
        /// The glyph positions.
        /// </value>
        public SKPoint[] GlyphPositions { get; }

        /// <summary>
        /// Gets the glyph clusters.
        /// </summary>
        /// <value>
        /// The glyph clusters.
        /// </value>
        public IReadOnlyList<SKGlyphCluster> GlyphClusters { get; }
    }
}
