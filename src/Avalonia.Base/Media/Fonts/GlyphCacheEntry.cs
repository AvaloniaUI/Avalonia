using System;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// One cached glyph representation in a <see cref="GlyphCache"/>: the built payload plus the
    /// metadata the cache needs to bound memory (<see cref="Cost"/>) and keep composites consistent
    /// (<see cref="Dependencies"/>). Immutable apart from the cache's own eviction bookkeeping.
    /// </summary>
    internal sealed class GlyphCacheEntry
    {
        public GlyphCacheEntry(ushort glyph, GlyphPayloadKind kind, object? payload, int cost,
            ushort[] dependencies, Rect bounds)
        {
            Glyph = glyph;
            Kind = kind;
            Payload = payload;
            // Cost is the eviction weight (estimated retained bytes); never zero, so budget arithmetic
            // can't be gamed by a glyph that reports no cost.
            Cost = cost < 1 ? 1 : cost;
            Dependencies = dependencies;
            Bounds = bounds;
        }

        /// <summary>The glyph this entry was built for (its key in the cache).</summary>
        public ushort Glyph { get; }

        /// <summary>The payload shape; tells a consumer how to interpret <see cref="Payload"/>.</summary>
        public GlyphPayloadKind Kind { get; }

        /// <summary>
        /// The built representation — an <c>IGeometryImpl</c> outline today, a color drawing or bitmap
        /// later. <c>null</c> is a valid memoised result (a malformed or outline-less glyph), which is
        /// why presence in the cache, not this reference, marks "already built".
        /// </summary>
        public object? Payload { get; }

        /// <summary>Estimated retained size in bytes; the weight charged against the cache budget.</summary>
        public int Cost { get; }

        /// <summary>
        /// Component / layer glyph IDs this entry is built from, or <see cref="Array.Empty{T}"/> for a
        /// simple glyph. Used to keep components at least as recent as the composites that use them.
        /// </summary>
        public ushort[] Dependencies { get; }

        /// <summary>The payload's bounding box, in font design units (Y-up).</summary>
        public Rect Bounds { get; }

        // --- Eviction bookkeeping. Owned by the cache and its IGlyphEvictionPolicy; not part of the
        //     entry's logical value. ---

        /// <summary>
        /// Recency flag (the CLOCK "referenced" bit). Set lock-free on a cache hit, cleared by the
        /// eviction sweep. An <see cref="int"/> so it can be read / written with <see cref="System.Threading.Volatile"/>.
        /// </summary>
        internal int Referenced;

        /// <summary>
        /// Number of live cached dependents pinning this entry. While positive the entry is never
        /// evicted — used only by <em>referencing</em> payloads (see <see cref="GlyphPayloadKind.ColorDrawing"/>),
        /// so a flattened outline component is not double-retained.
        /// </summary>
        internal int PinCount;

        // Intrusive doubly-linked ring node for the eviction policy (CLOCK hand / LRU order).
        internal GlyphCacheEntry? Prev;
        internal GlyphCacheEntry? Next;
    }
}
