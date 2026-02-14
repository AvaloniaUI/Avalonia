using Avalonia.Media.Fonts.Tables.Colr;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts.Tables.Glyf
{
    /// <summary>
    /// Type alias for the glyph decycler that tracks visited glyphs during composite glyph processing.
    /// </summary>
    internal class GlyphDecycler : Decycler<int>
    {
        /// <summary>
        /// Maximum depth for glyph graph traversal.
        /// This limit prevents stack overflow from deeply nested composite glyphs.
        /// </summary>
        public const int MaxTraversalDepth = 64;

        private static readonly ObjectPool<GlyphDecycler> Pool = new ObjectPool<GlyphDecycler>(
            factory: () => new GlyphDecycler(),
            validator: decycler =>
            {
                decycler.Reset();
                return true;
            },
            maxSize: 16);

        public GlyphDecycler() : base(MaxTraversalDepth)
        {
        }

        /// <summary>
        /// Rents a GlyphDecycler from the pool.
        /// </summary>
        /// <returns>A pooled GlyphDecycler instance.</returns>
        public static GlyphDecycler Rent()
        {
            return Pool.Rent();
        }

        /// <summary>
        /// Returns a GlyphDecycler to the pool.
        /// </summary>
        /// <param name="decycler">The decycler to return to the pool.</param>
        public static void Return(GlyphDecycler decycler)
        {
            Pool.Return(decycler);
        }
    }
}
