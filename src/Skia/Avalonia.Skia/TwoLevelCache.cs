using System;
using System.Collections.Generic;

namespace Avalonia.Skia
{
    /// <summary>
    /// Provides a lightweight two-level cache for storing key-value pairs, supporting fast retrieval and optional
    /// eviction handling.
    /// </summary>
    /// <remarks>The cache maintains a primary entry for the most recently added item and a secondary array
    /// for additional items, with a configurable capacity. When the cache exceeds its capacity, evicted values can be
    /// processed using an optional eviction action. This class is intended for internal use and is not
    /// thread-safe.</remarks>
    /// <typeparam name="TKey">The type of keys used to identify cached values. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of values to be stored in the cache. Must be a reference type.</typeparam>
    internal class TwoLevelCache<TKey, TValue>
        where TKey : notnull
        where TValue : class
    {
        private readonly int _secondarySize;
        private TKey? _primaryKey;
        private TValue? _primaryValue;
        private KeyValuePair<TKey, TValue>[]? _secondary;
        private int _secondaryCount;
        private readonly Action<TValue?>? _evictionAction;
        private readonly IEqualityComparer<TKey> _comparer;

        public TwoLevelCache(int secondarySize = 3, Action<TValue?>? evictionAction = null, IEqualityComparer<TKey>? comparer = null)
        {
            if (secondarySize < 0)
            { 
                throw new ArgumentOutOfRangeException(nameof(secondarySize));
            }

            _secondarySize = secondarySize;
            _evictionAction = evictionAction;
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public bool TryGet(TKey key, out TValue? value)
        {
            if (_primaryValue != null && _comparer.Equals(_primaryKey!, key))
            {
                value = _primaryValue;
                return true;
            }

            var sec = _secondary;
            if (sec != null)
            {
                for (int i = 0; i < _secondaryCount; i++)
                {
                    if (_comparer.Equals(sec[i].Key, key))
                    {
                        value = sec[i].Value;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            // Check if key already exists
            if (TryGet(key, out var existing) && existing != null)
            {
                return existing;
            }

            // Key doesn't exist, create new value
            var value = factory(key);

            // Primary is empty - store in primary
            if (_primaryValue == null)
            {
                _primaryKey = key;
                _primaryValue = value;
                return value;
            }

            // No secondary cache configured - replace primary
            if (_secondarySize == 0)
            {
                _evictionAction?.Invoke(_primaryValue);
                _primaryKey = key;
                _primaryValue = value;
                return value;
            }

            // Secondary not yet initialized - create it
            if (_secondary == null)
            {
                _secondary = new KeyValuePair<TKey, TValue>[_secondarySize];
                _secondaryCount = 0;
            }

            // Shift existing entries right and insert new one at front
            // This maintains insertion order and evicts the oldest (last) entry when full
            TValue? evicted = default;
            bool shouldEvict = _secondaryCount == _secondarySize;
            
            if (shouldEvict)
            {
                // Cache is full, last entry will be evicted
                evicted = _secondary[_secondarySize - 1].Value;
            }
            
            // Shift existing entries to make room at index 0
            int shiftCount = shouldEvict ? _secondarySize - 1 : _secondaryCount;
            for (int i = shiftCount; i > 0; i--)
            {
                _secondary[i] = _secondary[i - 1];
            }
            
            // Insert new entry at front
            _secondary[0] = new KeyValuePair<TKey, TValue>(key, value);
            
            // Update count (capped at size)
            if (_secondaryCount < _secondarySize)
            {
                _secondaryCount++;
            }
            
            // Invoke eviction action if we evicted an entry
            if (shouldEvict)
            {
                _evictionAction?.Invoke(evicted);
            }

            return value;
        }

        public void ClearAndDispose()
        {
            if (_primaryValue != null)
            {
                _evictionAction?.Invoke(_primaryValue);
                _primaryValue = null;
                _primaryKey = default;
            }

            if (_secondary != null)
            {
                for (int i = 0; i < _secondaryCount; i++)
                {
                    _evictionAction?.Invoke(_secondary[i].Value);
                }

                _secondary = null;
                _secondaryCount = 0;
            }
        }
    }
}
