using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a single glyph.
    /// </summary>
    public readonly record struct GlyphInfo(ushort GlyphIndex, int GlyphCluster, double GlyphAdvance, Vector GlyphOffset = default)
    {
        internal static Comparer<GlyphInfo> ClusterAscendingComparer { get; } =
            Comparer<GlyphInfo>.Create((x, y) => x.GlyphCluster.CompareTo(y.GlyphCluster));

        internal static Comparer<GlyphInfo> ClusterDescendingComparer { get; } =
            Comparer<GlyphInfo>.Create((x, y) => y.GlyphCluster.CompareTo(x.GlyphCluster));

        /// <summary>
        /// Get the glyph index.
        /// </summary>
        public ushort GlyphIndex { get; } = GlyphIndex;

        /// <summary>
        /// Get the glyph cluster.
        /// </summary>
        public int GlyphCluster { get; } = GlyphCluster;

        /// <summary>
        /// Get the glyph advance.
        /// </summary>
        public double GlyphAdvance { get; } = GlyphAdvance;

        /// <summary>
        /// Get the glyph offset.
        /// </summary>
        public Vector GlyphOffset { get; } = GlyphOffset;
    }
}
