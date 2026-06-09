using System.Threading;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Decides the eviction order for a <see cref="GlyphCache"/>. Swappable so a strict-LRU variant can
    /// replace the default <see cref="ClockEvictionPolicy"/> if measurement warrants it.
    /// </summary>
    /// <remarks>
    /// <see cref="OnAccessed"/> is on the hot read path and is invoked <em>without</em> the cache lock —
    /// an implementation must keep it cheap and self-thread-safe (the CLOCK default only writes a
    /// <see cref="Volatile"/> flag). <see cref="OnAdded"/>, <see cref="OnRemoved"/> and
    /// <see cref="SelectVictim"/> are always called under the cache's eviction lock.
    /// </remarks>
    internal interface IGlyphEvictionPolicy
    {
        /// <summary>Registers a newly inserted entry in the eviction order.</summary>
        void OnAdded(GlyphCacheEntry entry);

        /// <summary>Records a cache hit. Hot path; may run lock-free.</summary>
        void OnAccessed(GlyphCacheEntry entry);

        /// <summary>Removes an evicted entry from the eviction order.</summary>
        void OnRemoved(GlyphCacheEntry entry);

        /// <summary>
        /// Returns the next entry to evict — one with <see cref="GlyphCacheEntry.PinCount"/> zero — or
        /// <c>null</c> if every entry is currently pinned and nothing can be freed.
        /// </summary>
        GlyphCacheEntry? SelectVictim();
    }

    /// <summary>
    /// CLOCK / second-chance eviction. A hit sets the entry's referenced bit (lock-free, no list
    /// mutation); eviction sweeps a circular list, giving a referenced entry a second chance by
    /// clearing its bit, skipping pinned entries, and reclaiming the first entry it finds with the bit
    /// already clear. Approximates LRU without paying a per-hit relink.
    /// </summary>
    internal sealed class ClockEvictionPolicy : IGlyphEvictionPolicy
    {
        private GlyphCacheEntry? _hand; // the next entry the sweep will inspect
        private int _count;

        public void OnAdded(GlyphCacheEntry entry)
        {
            // New entries arrive referenced, so they survive at least one sweep before becoming
            // eviction candidates.
            Volatile.Write(ref entry.Referenced, 1);

            if (_hand is null)
            {
                entry.Prev = entry;
                entry.Next = entry;
                _hand = entry;
            }
            else
            {
                // Splice in just behind the hand, so the most-recently-added is swept last.
                var tail = _hand.Prev!;
                tail.Next = entry;
                entry.Prev = tail;
                entry.Next = _hand;
                _hand.Prev = entry;
            }

            _count++;
        }

        public void OnAccessed(GlyphCacheEntry entry) => Volatile.Write(ref entry.Referenced, 1);

        public void OnRemoved(GlyphCacheEntry entry)
        {
            if (entry.Next == entry)
            {
                _hand = null; // removed the last entry
            }
            else
            {
                entry.Prev!.Next = entry.Next;
                entry.Next!.Prev = entry.Prev;

                if (_hand == entry)
                {
                    _hand = entry.Next;
                }
            }

            entry.Prev = null;
            entry.Next = null;
            _count--;
        }

        public GlyphCacheEntry? SelectVictim()
        {
            if (_hand is null)
            {
                return null;
            }

            // Two trips around the ring clear every referenced bit once, so a victim is found unless
            // every entry is pinned.
            var limit = _count * 2;
            var hand = _hand;

            for (var i = 0; i < limit; i++)
            {
                var next = hand!.Next!;

                if (Volatile.Read(ref hand.Referenced) != 0)
                {
                    Volatile.Write(ref hand.Referenced, 0); // second chance
                }
                else if (hand.PinCount == 0)
                {
                    _hand = next;
                    return hand;
                }

                hand = next;
            }

            _hand = hand;
            return null; // everything pinned
        }
    }
}
