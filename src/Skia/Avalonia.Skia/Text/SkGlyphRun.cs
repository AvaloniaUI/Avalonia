// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Skia
{
    using SkiaSharp;

    public class SKGlyphRun
    {
        private readonly List<SKGlyphCluster> _glyphClusters;

        public SKGlyphRun(byte[] glyphIds, SKPoint[] glyphPositions, List<SKGlyphCluster> glyphClusters)
        {
            GlyphIds = glyphIds;
            GlyphPositions = glyphPositions;
            _glyphClusters = glyphClusters;
        }

        public byte[] GlyphIds { get; }

        public SKPoint[] GlyphPositions { get; }

        public IReadOnlyList<SKGlyphCluster> GlyphClusters => _glyphClusters;
    }
}
