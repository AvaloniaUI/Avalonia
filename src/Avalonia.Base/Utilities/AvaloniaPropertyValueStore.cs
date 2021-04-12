using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Avalonia.Utilities
{
    /// <summary>
    /// Stores values with <see cref="AvaloniaProperty"/> as key.
    /// </summary>
    /// <typeparam name="TValue">Stored value type.</typeparam>
    internal sealed class AvaloniaPropertyValueStore<TValue>
    {
        // The last item in the list is always int.MaxValue.
        private static readonly Entry[] s_emptyEntries = { new Entry { PropertyId = int.MaxValue, Value = default! } };
        
        private Entry[] _entries;

        public AvaloniaPropertyValueStore()
        {
            _entries = s_emptyEntries;
        }

        public int Count => _entries.Length - 1;
        public TValue this[int index] => _entries[index].Value;

        private (int, bool) TryFindEntry(int propertyId)
        {
            if (_entries.Length <= 12)
            {
                // For small lists, we use an optimized linear search. Since the last item in the list
                // is always int.MaxValue, we can skip a conditional branch in each iteration.
                // By unrolling the loop, we can skip another unconditional branch in each iteration.

                if (_entries[0].PropertyId >= propertyId)
                    return (0, _entries[0].PropertyId == propertyId);
                if (_entries[1].PropertyId >= propertyId)
                    return (1, _entries[1].PropertyId == propertyId);
                if (_entries[2].PropertyId >= propertyId)
                    return (2, _entries[2].PropertyId == propertyId);
                if (_entries[3].PropertyId >= propertyId)
                    return (3, _entries[3].PropertyId == propertyId);
                if (_entries[4].PropertyId >= propertyId)
                    return (4, _entries[4].PropertyId == propertyId);
                if (_entries[5].PropertyId >= propertyId)
                    return (5, _entries[5].PropertyId == propertyId);
                if (_entries[6].PropertyId >= propertyId)
                    return (6, _entries[6].PropertyId == propertyId);
                if (_entries[7].PropertyId >= propertyId)
                    return (7, _entries[7].PropertyId == propertyId);
                if (_entries[8].PropertyId >= propertyId)
                    return (8, _entries[8].PropertyId == propertyId);
                if (_entries[9].PropertyId >= propertyId)
                    return (9, _entries[9].PropertyId == propertyId);
                if (_entries[10].PropertyId >= propertyId)
                    return (10, _entries[10].PropertyId == propertyId);
            }
            else
            {
                int low = 0;
                int high = _entries.Length;
                int id;

                while (high - low > 3)
                {
                    int pivot = (high + low) / 2;
                    id = _entries[pivot].PropertyId;

                    if (propertyId == id)
                        return (pivot, true);

                    if (propertyId <= id)
                        high = pivot;
                    else
                        low = pivot + 1;
                }

                do
                {
                    id = _entries[low].PropertyId;

                    if (id == propertyId)
                        return (low, true);

                    if (id > propertyId)
                        break;

                    ++low;
                }
                while (low < high);
            }

            return (0, false);
        }

        public bool TryGetValue(AvaloniaProperty property, [MaybeNullWhen(false)] out TValue value)
        {
            (int index, bool found) = TryFindEntry(property.Id);
            if (!found)
            {
                value = default;
                return false;
            }

            value = _entries[index].Value;
            return true;
        }

        public void AddValue(AvaloniaProperty property, TValue value)
        {
            Entry[] entries = new Entry[_entries.Length + 1];

            for (int i = 0; i < _entries.Length; ++i)
            {
                if (_entries[i].PropertyId > property.Id)
                {
                    if (i > 0)
                    {
                        Array.Copy(_entries, 0, entries, 0, i);
                    }

                    entries[i] = new Entry { PropertyId = property.Id, Value = value };
                    Array.Copy(_entries, i, entries, i + 1, _entries.Length - i);
                    break;
                }
            }

            _entries = entries;
        }

        public void SetValue(AvaloniaProperty property, TValue value)
        {
            _entries[TryFindEntry(property.Id).Item1].Value = value;
        }

        public void Remove(AvaloniaProperty property)
        {
            var (index, found) = TryFindEntry(property.Id);

            if (found)
            {
                var newLength = _entries.Length - 1;
                
                // Special case - one element left means that value store is empty so we can just reuse our "empty" array.
                if (newLength == 1)
                {
                    _entries = s_emptyEntries;
                    
                    return;
                }
                
                var entries = new Entry[newLength];

                int ix = 0;

                for (int i = 0; i < _entries.Length; ++i)
                {
                    if (i != index)
                    {
                        entries[ix++] = _entries[i];
                    }
                }

                _entries = entries;
            }
        }

        private struct Entry
        {
            internal int PropertyId;
            internal TValue Value;
        }
    }
}
