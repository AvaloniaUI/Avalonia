using Avalonia.Utilities;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Decycler for the COLR v1 paint graph, keyed on the absolute paint offset within the COLR
    /// table. Every recursive parse edge goes through <see cref="PaintParser.TryParse"/>, which
    /// enters this guard on the paint's offset — so both paint→paint cycles (an offset that
    /// reappears on the current path) and unbounded acyclic nesting (depth past
    /// <see cref="MaxTraversalDepth"/>) are caught, mirroring HarfBuzz's per-paint nesting cap.
    /// Offsets uniquely identify a paint (and each glyph resolves to one base-paint offset), so an
    /// offset key also subsumes the older glyph-boundary cycle check.
    /// </summary>
    internal class PaintDecycler : Decycler<uint>
    {
        /// <summary>
        /// Maximum depth for paint graph traversal.
        /// This limit matches HB_MAX_NESTING_LEVEL used in HarfBuzz.
        /// </summary>
        public const int MaxTraversalDepth = 64;

        private static readonly ObjectPool<PaintDecycler> Pool = new ObjectPool<PaintDecycler>(
            factory: () => new PaintDecycler(),
            validator: decycler =>
            {
                decycler.Reset();
                return true;
            },
            maxSize: 32);

        public PaintDecycler() : base(MaxTraversalDepth)
        {
        }

        /// <summary>
        /// Rents a PaintDecycler from the pool.
        /// </summary>
        /// <returns>A pooled PaintDecycler instance.</returns>
        public static PaintDecycler Rent()
        {
            return Pool.Rent();
        }

        /// <summary>
        /// Returns a PaintDecycler to the pool.
        /// </summary>
        /// <param name="decycler">The decycler to return to the pool.</param>
        public static void Return(PaintDecycler decycler)
        {
            Pool.Return(decycler);
        }
    }
}
