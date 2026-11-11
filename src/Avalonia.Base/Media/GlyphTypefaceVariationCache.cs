using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Avalonia.Media
{
    /// <summary>
    /// The variation clones of one default-instance <see cref="GlyphTypeface"/>, keyed by position.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every clone that is still alive is reachable by its position, so a position returns the same
    /// clone for as long as anyone holds it. The cache itself holds <see cref="RecentlyUsedCapacity"/>
    /// clones strongly: a new clone enters that set, and a clone in it that is requested again keeps
    /// its place (CLOCK). Every other clone stays alive only through its outside holders and is
    /// collected once those drop it. An animated axis asks for a new position on every frame, so
    /// holding every clone would retain one per frame value for the life of the font.
    /// </para>
    /// <para>
    /// A clone that leaves the strongly held set is not disposed: a text layout or glyph run can
    /// still shape or draw with it. Native state the clone owns, such as its shaping font, is
    /// released by that state's own finalizer once the clone is collected. The default-instance
    /// typeface disposes the clones still alive when it is disposed.
    /// </para>
    /// <para>
    /// Hits are lock-free. A miss creates the clone under the cache lock, so concurrent requests for
    /// one position create it once.
    /// </para>
    /// </remarks>
    internal sealed class GlyphTypefaceVariationCache
    {
        /// <summary>The number of clones the cache holds strongly.</summary>
        public const int RecentlyUsedCapacity = 16;

        private readonly ConcurrentDictionary<NormalizedVariationPosition, Slot> _slots = new();
        private readonly Slot?[] _recent = new Slot?[RecentlyUsedCapacity];
        private readonly object _lock = new();
        private int _hand;

        // The number of slots in the map, kept under the lock because ConcurrentDictionary.Count
        // takes every stripe lock of the map.
        private int _count;

        // Dead slots are swept once the map reaches this size, which then becomes twice the number
        // of slots left, so the sweep cost stays proportional to the number of misses.
        private int _sweepAt = RecentlyUsedCapacity * 2;

        /// <summary>Returns the clone for <paramref name="position"/>, creating it on a miss.</summary>
        public GlyphTypeface GetOrAdd<TState>(NormalizedVariationPosition position,
            Func<NormalizedVariationPosition, TState, GlyphTypeface> create, TState state)
        {
            if (_slots.TryGetValue(position, out var slot))
            {
                var strong = Volatile.Read(ref slot.Strong);

                if (strong is not null)
                {
                    // Write the bit only when it is clear, so steady hits do not keep dirtying the
                    // slot's cache line.
                    if (Volatile.Read(ref slot.Referenced) == 0)
                    {
                        Volatile.Write(ref slot.Referenced, 1);
                    }

                    return strong;
                }

                // A live clone outside the strongly held set is held by its users, so it needs no
                // place in the set while they hold it. Once they drop it, it is collected and the
                // next request is a miss that puts the new clone in the set.
                if (slot.Clone.TryGetTarget(out var alive))
                {
                    return alive;
                }
            }

            lock (_lock)
            {
                if (_slots.TryGetValue(position, out slot) && slot.Clone.TryGetTarget(out var clone))
                {
                    return clone;
                }

                clone = create(position, state);

                if (slot is null)
                {
                    _count++;
                }

                slot = new Slot(clone);
                _slots[position] = slot;
                Retain(slot, clone);

                if (_count >= _sweepAt)
                {
                    SweepCollected();
                }

                return clone;
            }
        }

        /// <summary>
        /// Removes every clone from the cache and returns the ones that are still alive, for the
        /// default-instance typeface to dispose.
        /// </summary>
        public List<GlyphTypeface> Clear()
        {
            lock (_lock)
            {
                var alive = new List<GlyphTypeface>(_count);

                foreach (var pair in _slots)
                {
                    if (pair.Value.Clone.TryGetTarget(out var clone))
                    {
                        alive.Add(clone);
                    }
                }

                _slots.Clear();
                _count = 0;
                Array.Clear(_recent, 0, _recent.Length);

                return alive;
            }
        }

        // Puts a new slot into the strongly held set, displacing a CLOCK victim. A slot that was
        // requested since the hand last passed it gets a second chance. The new slot enters
        // unreferenced: if every slot entered referenced, a miss would sweep the whole ring and
        // evict the slot it started at, so the set would keep the newest clones however often an
        // older one is requested.
        private void Retain(Slot slot, GlyphTypeface clone)
        {
            while (true)
            {
                var index = _hand;
                _hand = (index + 1) % _recent.Length;

                var occupant = _recent[index];

                if (occupant is not null && Volatile.Read(ref occupant.Referenced) != 0)
                {
                    Volatile.Write(ref occupant.Referenced, 0);
                    continue;
                }

                if (occupant is not null)
                {
                    Volatile.Write(ref occupant.Strong, null);
                }

                _recent[index] = slot;
                Volatile.Write(ref slot.Strong, clone);
                return;
            }
        }

        private void SweepCollected()
        {
            foreach (var pair in _slots)
            {
                if (!pair.Value.Clone.TryGetTarget(out _) && _slots.TryRemove(pair))
                {
                    _count--;
                }
            }

            _sweepAt = Math.Max(RecentlyUsedCapacity * 2, _count * 2);
        }

        private sealed class Slot
        {
            public Slot(GlyphTypeface clone)
            {
                Clone = new WeakReference<GlyphTypeface>(clone);
            }

            public readonly WeakReference<GlyphTypeface> Clone;

            // Non-null while the slot is in the strongly held set; written under the cache lock.
            public GlyphTypeface? Strong;

            public int Referenced;
        }
    }
}
