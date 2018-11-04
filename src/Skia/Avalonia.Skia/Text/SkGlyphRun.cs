// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

using SkiaSharp;

namespace Avalonia.Skia
{
    public class SKGlyphRun
    {
        public SKGlyphRun(byte[] glyphIds, SKPoint[] glyphPositions, IReadOnlyList<SKGlyphCluster> glyphClusters)
        {
            GlyphIds = glyphIds;
            GlyphPositions = glyphPositions;
            GlyphClusters = glyphClusters;
        }

        public byte[] GlyphIds { get; }

        public SKPoint[] GlyphPositions { get; }

        public IReadOnlyList<SKGlyphCluster> GlyphClusters { get; }
    }
}
