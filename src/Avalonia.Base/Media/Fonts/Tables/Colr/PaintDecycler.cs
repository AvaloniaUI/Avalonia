namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Decycler for the COLR v1 paint graph of one glyph, keyed on the absolute paint offset within
    /// the COLR table. Every recursive parse edge goes through <see cref="PaintParser.TryParse"/>,
    /// which enters this guard on the paint's offset, so paint-to-paint cycles (an offset that
    /// reappears on the current path) and unbounded acyclic nesting (depth past
    /// <see cref="MaxTraversalDepth"/>) are caught, mirroring HarfBuzz's per-paint nesting cap.
    /// </summary>
    /// <remarks>
    /// Cycle detection is path-scoped, so a DAG whose nodes are shared by many parents (for example
    /// PaintColrLayers entries that all reference one nested PaintColrLayers) passes it while
    /// expanding exponentially. <see cref="CountEdge"/> bounds the total number of paints parsed
    /// for one glyph, like HarfBuzz's HB_COLRV1_MAX_EDGE_COUNT. Use one instance per glyph parse.
    /// </remarks>
    internal sealed class PaintDecycler : Decycler<uint>
    {
        /// <summary>
        /// Maximum depth for paint graph traversal.
        /// This limit matches HB_MAX_NESTING_LEVEL used in HarfBuzz.
        /// </summary>
        public const int MaxTraversalDepth = 64;

        /// <summary>
        /// Maximum number of paints parsed for one glyph, matching HarfBuzz's
        /// HB_COLRV1_MAX_EDGE_COUNT.
        /// </summary>
        public const int MaxEdgeCount = 65536;

        private int _edgeCount;

        public PaintDecycler() : base(MaxTraversalDepth)
        {
        }

        /// <summary>
        /// Counts one paint edge against the per-glyph budget.
        /// </summary>
        /// <exception cref="DecyclerException">The budget of <see cref="MaxEdgeCount"/> is exhausted.</exception>
        public void CountEdge()
        {
            if (++_edgeCount > MaxEdgeCount)
            {
                throw new DecyclerException(
                    DecyclerError.DepthLimitExceeded,
                    $"Paint graph edge budget of {MaxEdgeCount} exceeded");
            }
        }
    }
}
