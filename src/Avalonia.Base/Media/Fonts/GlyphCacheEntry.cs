using System;
using System.Threading;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// One glyph's entry in a <see cref="GlyphCache"/>. Holds the cheap ink box (set lazily and kept)
    /// and the heavy outline geometry (built lazily on demand and evictable under the cache budget).
    /// Splitting the two lets the metrics path read bounds without ever building geometry, and lets
    /// eviction drop only the geometry while retaining the tiny bounds.
    /// </summary>
    internal sealed class GlyphCacheEntry
    {
        public GlyphCacheEntry(ushort glyph, bool retainBounds)
        {
            Glyph = glyph;
            RetainBounds = retainBounds;
            Dependencies = Array.Empty<ushort>();
        }

        /// <summary>The glyph this entry is for (its key in the cache).</summary>
        public ushort Glyph { get; }

        /// <summary>
        /// Whether the entry should survive eviction of its geometry, for the sake of its cached bounds.
        /// True for CFF / CFF2 (whose bounds are expensive to recompute); false for glyf (whose bounds
        /// are a cheap header read and so are never cached here).
        /// </summary>
        public bool RetainBounds { get; }

        // --- Ink box: set lazily by whichever path needs it first (the metrics interpret, or the
        //     geometry build), then immutable. A benign race may set it twice with identical values;
        //     an 8-byte GlyphBounds store is atomic on 64-bit. ---

        private GlyphBounds _bounds;
        private int _hasBounds;

        public bool HasBounds => Volatile.Read(ref _hasBounds) != 0;

        public GlyphBounds Bounds => _bounds;

        public void SetBoundsOnce(GlyphBounds bounds)
        {
            if (Volatile.Read(ref _hasBounds) != 0)
            {
                return;
            }

            _bounds = bounds;
            Volatile.Write(ref _hasBounds, 1);
        }

        // --- Outline geometry: built lazily under the cache lock (so cost is charged atomically with
        //     eviction), dropped on eviction. A null Geometry with HasGeometry set is a valid result
        //     (a malformed glyph); a null Geometry with HasGeometry clear means "not built / evicted". ---

        internal object? Geometry;
        private int _hasGeometry;

        public bool HasGeometry => Volatile.Read(ref _hasGeometry) != 0;

        public GlyphPayloadKind Kind { get; private set; }

        public ushort[] Dependencies { get; private set; }

        /// <summary>Estimated retained bytes of the geometry; the cache eviction weight. Zero when unbuilt.</summary>
        public int Cost { get; private set; }

        internal void SetGeometry(object? geometry, int cost, GlyphPayloadKind kind, ushort[] dependencies)
        {
            Kind = kind;
            Dependencies = dependencies;
            Cost = cost;
            Volatile.Write(ref Geometry, geometry);   // publish the reference before the flag
            Volatile.Write(ref _hasGeometry, 1);
        }

        internal void ClearGeometry()
        {
            // Clear the flag before nulling the reference so a lock-free reader never sees the
            // "built but null" (malformed) state for an evicted entry.
            Volatile.Write(ref _hasGeometry, 0);
            Volatile.Write(ref Geometry, null);
            Cost = 0;
            Referenced = 0;
        }

        // --- Eviction bookkeeping (owned by the cache + its IGlyphEvictionPolicy). ---

        internal int Referenced;
        internal int PinCount;
        internal GlyphCacheEntry? Prev;
        internal GlyphCacheEntry? Next;
    }
}
