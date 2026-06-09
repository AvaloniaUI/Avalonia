using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// A bounded, per-typeface cache of built glyph payloads (outlines today; color drawings and
    /// bitmaps later). The total retained <see cref="GlyphCacheEntry.Cost"/> is capped by a byte
    /// budget; an <see cref="IGlyphEvictionPolicy"/> chooses what to drop when the budget is exceeded.
    /// </summary>
    /// <remarks>
    /// Reads are lock-free (a concurrent lookup plus a <see cref="Volatile"/> recency write); inserts
    /// and the eviction sweep take a single lock. Composite components are kept at least as recent as
    /// the composites that use them via recency propagation; <em>referencing</em> payloads additionally
    /// pin their dependencies so a live dependent can never lose a component it still points at.
    /// </remarks>
    internal sealed class GlyphCache
    {
        /// <summary>Default per-typeface budget: a few MB of glyph payloads. Calibrated against the churn benchmark.</summary>
        public const int DefaultBudgetBytes = 4 * 1024 * 1024;

        private readonly ConcurrentDictionary<ushort, GlyphCacheEntry> _entries = new();
        private readonly IGlyphEvictionPolicy _policy;
        private readonly int _budget;
        private readonly object _lock = new();
        private int _totalCost;

        public GlyphCache(int budgetBytes = DefaultBudgetBytes, IGlyphEvictionPolicy? policy = null)
        {
            _budget = budgetBytes < 1 ? 1 : budgetBytes;
            _policy = policy ?? new ClockEvictionPolicy();
        }

        /// <summary>Number of cached entries.</summary>
        public int Count => _entries.Count;

        /// <summary>Total retained cost (bytes) of the cached entries.</summary>
        public int TotalCost => Volatile.Read(ref _totalCost);

        /// <summary>
        /// Looks up <paramref name="glyph"/>. On a hit, marks the entry (and its cached dependencies)
        /// recently used. Lock-free.
        /// </summary>
        public bool TryGet(ushort glyph, out GlyphCacheEntry entry)
        {
            if (_entries.TryGetValue(glyph, out entry!))
            {
                _policy.OnAccessed(entry);
                PropagateRecency(entry);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the cached entry for <paramref name="glyph"/>, building it with
        /// <paramref name="factory"/> on a miss. The factory runs outside the lock; if two threads race
        /// the first published entry wins and the loser's payload is disposed if disposable.
        /// </summary>
        public GlyphCacheEntry GetOrAdd(ushort glyph, Func<ushort, GlyphCacheEntry> factory)
        {
            if (TryGet(glyph, out var existing))
            {
                return existing;
            }

            var built = factory(glyph);

            lock (_lock)
            {
                if (_entries.TryGetValue(glyph, out var winner))
                {
                    // Lost the race — discard ours, keep the published one.
                    DisposePayload(built);
                    _policy.OnAccessed(winner);
                    return winner;
                }

                _entries[glyph] = built;
                _policy.OnAdded(built);
                _totalCost += built.Cost;

                PinDependencies(built);
                EvictToBudget();

                return built;
            }
        }

        // Keep a composite's cached components at least as recently-used as the composite itself, so a
        // component is never evicted before a composite that depends on it. A no-op for the common
        // dependency-free glyph.
        private void PropagateRecency(GlyphCacheEntry entry)
        {
            var deps = entry.Dependencies;

            for (var i = 0; i < deps.Length; i++)
            {
                if (_entries.TryGetValue(deps[i], out var dep))
                {
                    _policy.OnAccessed(dep);
                }
            }
        }

        // Pin the dependencies of a referencing payload so they cannot be evicted while this entry is
        // live. Gated on Kind: a flattened outline composite already contains its components, so pinning
        // them would double-retain memory — those rely on recency only.
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

        // Only payloads that hold live references to their components pin them. Flattened outlines
        // (Outline / CompositeOutline) are self-contained.
        private static bool IsReferencing(GlyphPayloadKind kind) => kind == GlyphPayloadKind.ColorDrawing;

        private void EvictToBudget()
        {
            while (_totalCost > _budget)
            {
                var victim = _policy.SelectVictim();

                if (victim is null)
                {
                    // Everything is pinned by a live dependent — stay over budget until one is freed.
                    break;
                }

                _entries.TryRemove(victim.Glyph, out _);
                _policy.OnRemoved(victim);
                _totalCost -= victim.Cost;
                UnpinDependencies(victim);
                DisposePayload(victim);
            }
        }

        private static void DisposePayload(GlyphCacheEntry entry)
        {
            if (entry.Payload is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
