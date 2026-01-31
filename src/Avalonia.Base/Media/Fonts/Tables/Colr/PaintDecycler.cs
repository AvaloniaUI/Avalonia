using Avalonia.Utilities;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Type alias for the paint decycler that tracks visited glyphs.
    /// </summary>
    internal class PaintDecycler : Decycler<ushort>
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
