using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// The result of building a glyph's outline geometry, handed back to <see cref="GlyphCache"/>.
    /// The payload must be an immutable, non-disposable object (see the cache remarks).
    /// </summary>
    internal readonly struct BuiltGeometry
    {
        public BuiltGeometry(object? geometry, int cost, GlyphPayloadKind kind, ushort[] dependencies,
            GlyphBounds bounds, bool hasBounds)
        {
            Geometry = geometry;
            Cost = cost;
            Kind = kind;
            Dependencies = dependencies;
            Bounds = bounds;
            HasBounds = hasBounds;
        }

        public object? Geometry { get; }
        public int Cost { get; }
        public GlyphPayloadKind Kind { get; }
        public ushort[] Dependencies { get; }
        public GlyphBounds Bounds { get; }
        public bool HasBounds { get; }
    }

    /// <summary>
    /// A bounded, per-typeface cache keyed by glyph. Each <see cref="GlyphCacheEntry"/> carries the
    /// cheap ink box (set lazily, kept for the metrics fast path) and a heavy outline geometry built
    /// lazily on demand. Only the geometry counts against the byte budget; eviction (CLOCK) drops the
    /// geometry but keeps the entry's bounds when they are worth retaining (CFF / CFF2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bounds reads and built-geometry hits are lock-free; the geometry build / eviction take a lock.
    /// A hit on a composite also marks its cached direct components referenced; referencing payloads
    /// (color drawings) additionally pin their dependencies.
    /// </para>
    /// <para>
    /// Payloads must be immutable, non-disposable managed objects: hits are handed out lock-free and
    /// escape with unbounded lifetime (public callers, retained compositor render data), so eviction
    /// only unlinks. The GC reclaims a payload, and any native memory behind it through finalizers,
    /// once the last outside reference drops. Anything that needs deterministic teardown must not be
    /// cached here.
    /// </para>
    /// </remarks>
    internal sealed class GlyphCache
    {
        /// <summary>
        /// Default per-typeface geometry budget, in estimated bytes. At the outline cost estimate a
        /// glyph of about 30 segments costs about 1 KB, so this keeps a few thousand outlines per
        /// typeface.
        /// </summary>
        public const int DefaultBudgetBytes = 4 * 1024 * 1024;

        private static readonly Func<ushort, bool, GlyphCacheEntry> s_createEntry =
            static (glyph, retainBounds) => new GlyphCacheEntry(glyph, retainBounds);

        private readonly ConcurrentDictionary<ushort, GlyphCacheEntry> _entries = new();

        // Colour drawings are keyed by the same glyph id as outlines but are a distinct representation
        // (a COLR drawing whose layer outlines live in _entries), so they get their own map: a colour
        // base glyph and its own fallback outline can coexist without clobbering one slot. Both maps
        // share the eviction ring and the byte budget.
        private readonly ConcurrentDictionary<ushort, GlyphCacheEntry> _colorEntries = new();

        private readonly IGlyphEvictionPolicy _policy;
        private readonly int _budget;
        private readonly bool _retainBounds;
        private readonly object _lock = new();
        private int _totalCost;

        /// <param name="retainBounds">
        /// Whether entries survive geometry eviction for the sake of their cached bounds. This is a
        /// per-typeface constant: <c>true</c> where bounds are expensive to recompute (CFF / CFF2),
        /// <c>false</c> where they are a cheap header read (glyf).
        /// </param>
        /// <param name="budgetBytes">The geometry byte budget eviction keeps the cache under.</param>
        /// <param name="policy">The eviction policy; CLOCK when omitted.</param>
        public GlyphCache(bool retainBounds = false, int budgetBytes = DefaultBudgetBytes,
            IGlyphEvictionPolicy? policy = null)
        {
            _retainBounds = retainBounds;
            _budget = budgetBytes < 1 ? 1 : budgetBytes;
            _policy = policy ?? new ClockEvictionPolicy();
        }

        /// <summary>Number of cached entries (outline and colour-drawing, with or without built geometry).</summary>
        public int Count => _entries.Count + _colorEntries.Count;

        /// <summary>Total retained geometry cost (bytes).</summary>
        public int TotalCost => Volatile.Read(ref _totalCost);

        /// <summary>
        /// Gets (creating if absent) the entry for <paramref name="glyph"/>. Whether the entry survives
        /// geometry eviction for its cached bounds is the cache's <c>retainBounds</c> setting. Lock-free.
        /// </summary>
        public GlyphCacheEntry GetEntry(ushort glyph)
            => _entries.GetOrAdd(glyph, s_createEntry, _retainBounds);

        /// <summary>
        /// Gets (creating if absent) the colour-drawing entry for <paramref name="glyph"/>, in the
        /// dedicated colour map. Colour drawings carry no retained bounds (the drawing exposes its own),
        /// so the entry is dropped whole on eviction. Lock-free.
        /// </summary>
        public GlyphCacheEntry GetColorEntry(ushort glyph)
            => _colorEntries.GetOrAdd(glyph, s_createEntry, false);

        /// <summary>
        /// Returns the built outline geometry for <paramref name="entry"/>, building it with
        /// <paramref name="build"/> on a miss (charging its cost and evicting to budget). A built hit is
        /// lock-free; a malformed glyph caches as a built <c>null</c>.
        /// </summary>
        public object? GetOrBuildGeometry(GlyphCacheEntry entry, Func<GlyphCacheEntry, BuiltGeometry> build)
        {
            var geometry = Volatile.Read(ref entry.Geometry);
            if (geometry != null)
            {
                _policy.OnAccessed(entry);
                PropagateRecency(entry);
                return geometry;
            }

            if (entry.HasGeometry)
            {
                // The built flag is published after the geometry and cleared before it, so a re-read
                // under a set flag is exact: non-null means the null read above raced a concurrent
                // build; null with the flag still set means the glyph is genuinely malformed; null
                // with the flag now clear means the entry was evicted between the reads, so fall
                // through and rebuild under the lock.
                geometry = Volatile.Read(ref entry.Geometry);

                if (geometry != null)
                {
                    _policy.OnAccessed(entry);
                    PropagateRecency(entry);
                    return geometry;
                }

                if (entry.HasGeometry)
                {
                    // Keep the memoised miss warm too, or malformed entries are evicted first under
                    // pressure and re-parsed on every request.
                    _policy.OnAccessed(entry);
                    return null;   // built, but the glyph has no outline (malformed)
                }
            }

            lock (_lock)
            {
                if (entry.HasGeometry)
                {
                    return entry.Geometry;   // another thread built it while we waited
                }

                var built = build(entry);

                if (built.HasBounds)
                {
                    entry.SetBoundsOnce(built.Bounds);
                }

                entry.SetGeometry(built.Geometry, built.Cost, built.Kind, built.Dependencies);
                _policy.OnAdded(entry);
                _totalCost += built.Cost;

                PinDependencies(entry);
                EvictToBudget();

                return built.Geometry;
            }
        }

        /// <summary>
        /// Returns the cached colour-drawing payload for <paramref name="entry"/>, building it with
        /// <paramref name="build"/> on a miss. The payload (a COLR drawing) is parsed <em>outside</em>
        /// the lock: parsing builds no geometry — so it needs no render backend, unlike the layer
        /// outlines it references — and holding the lock across the per-layer ink-box reads would
        /// serialise the whole cache. A built hit is lock-free; two racing parses is acceptable (one
        /// wins at insertion, the other is discarded). On insertion the drawing pins whichever of its
        /// layer outlines are already cached; the rest are kept warm by recency until they are.
        /// </summary>
        public object? GetOrBuildDrawing(GlyphCacheEntry entry, Func<GlyphCacheEntry, BuiltGeometry> build)
        {
            var existing = Volatile.Read(ref entry.Geometry);
            if (existing != null)
            {
                _policy.OnAccessed(entry);
                PropagateRecency(entry);
                return existing;
            }

            if (entry.HasGeometry)
            {
                return null;   // built, but the glyph has no colour drawing
            }

            var built = build(entry);

            lock (_lock)
            {
                if (entry.HasGeometry)
                {
                    return entry.Geometry;   // another thread built it while we parsed
                }

                entry.SetGeometry(built.Geometry, built.Cost, built.Kind, built.Dependencies);
                _policy.OnAdded(entry);
                _totalCost += built.Cost;

                PinDependencies(entry);
                EvictToBudget();

                return built.Geometry;
            }
        }

        // Mark a composite's cached direct components referenced on every hit of the composite, so
        // CLOCK gives them the same second chance. This approximates keeping a component at least as
        // recent as its composites; it does not guarantee eviction order, and components of
        // components are not reached.
        private void PropagateRecency(GlyphCacheEntry entry)
        {
            var deps = entry.Dependencies;

            for (var i = 0; i < deps.Length; i++)
            {
                if (_entries.TryGetValue(deps[i], out var dep) && dep.HasGeometry)
                {
                    _policy.OnAccessed(dep);
                }
            }
        }

        private void PinDependencies(GlyphCacheEntry entry)
        {
            if (!IsReferencing(entry.Kind))
            {
                return;
            }

            var deps = entry.Dependencies;

            for (var i = 0; i < deps.Length; i++)
            {
                if (_entries.TryGetValue(deps[i], out var dep))
                {
                    dep.PinCount++;
                }
            }
        }

        private void UnpinDependencies(GlyphCacheEntry entry)
        {
            if (!IsReferencing(entry.Kind))
            {
                return;
            }

            var deps = entry.Dependencies;

            for (var i = 0; i < deps.Length; i++)
            {
                if (_entries.TryGetValue(deps[i], out var dep) && dep.PinCount > 0)
                {
                    dep.PinCount--;
                }
            }
        }

        // Only payloads that hold live references to their components pin them; flattened outlines are
        // self-contained and rely on recency.
        private static bool IsReferencing(GlyphPayloadKind kind) => kind == GlyphPayloadKind.ColorDrawing;

        private void EvictToBudget()
        {
            while (_totalCost > _budget)
            {
                var victim = _policy.SelectVictim();

                if (victim is null)
                {
                    break;   // everything pinned: stay over budget until a dependent frees
                }

                _totalCost -= victim.Cost;
                _policy.OnRemoved(victim);
                UnpinDependencies(victim);
                victim.ClearGeometry();

                // Keep the entry alive for its retained bounds (CFF / CFF2); otherwise drop it from its
                // owning map (colour drawings never retain bounds, so they are always dropped whole).
                // Remove only this entry: an orphan (already dropped from the map, then rebuilt by a
                // caller that still held it) must not take a fresh entry for the same key with it.
                if (!victim.RetainBounds)
                {
                    var map = victim.Kind == GlyphPayloadKind.ColorDrawing ? _colorEntries : _entries;
                    map.TryRemove(new KeyValuePair<ushort, GlyphCacheEntry>(victim.Glyph, victim));
                }

                // No Dispose here: the payload may still be referenced by anyone the lock-free hit
                // path handed it to, or by retained compositor render data, so deterministic teardown
                // would be a use-after-dispose. The GC reclaims it once the last reference drops.
            }
        }
    }
}
