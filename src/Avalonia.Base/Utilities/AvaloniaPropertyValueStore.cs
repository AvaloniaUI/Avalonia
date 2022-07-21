using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Stores values with <see cref="AvaloniaProperty"/> as key.
    /// </summary>
    /// <typeparam name="TValue">Stored value type.</typeparam>
    internal struct AvaloniaPropertyValueStore<TValue>
    {
        private const int DefaultInitialCapacity = 4;
        private Entry[]? _entries;
        private int _entryCount;

        public AvaloniaPropertyValueStore()
        {
            _entries = null;
            _entryCount = 0;
        }

        public AvaloniaPropertyValueStore(int capactity)
        {
            _entries = new Entry[capactity];
            _entryCount = 0;
        }

        public int Count => _entryCount;
        
        public TValue this[int index] => _entries![index].Value;

        private bool TryGetEntry(int propertyId, out int index)
        {
            int checkIndex;
            int iLo = 0;
            int iHi = _entryCount;

            if (iHi <= 0)
            {
                index = 0;
                return false;
            }

            // Do a binary search to find the value
            while (iHi - iLo > 3)
            {
                int iPv = (iHi + iLo) / 2;
                checkIndex = _entries![iPv].PropertyId;

                if (propertyId == checkIndex)
                {
                    index = iPv;
                    return true;
                }

                if (propertyId <= checkIndex)
                {
                    iHi = iPv;
                }
                else
                {
                    iLo = iPv + 1;
                }
            }

            // Now we only have three values to search; switch to a linear search
            do
            {
                checkIndex = _entries![iLo].PropertyId;

                if (checkIndex == propertyId)
                {
                    index = iLo;
                    return true;
                }

                if (checkIndex > propertyId)
                {
                    // we've gone past the targetIndex - return not found
                    break;
                }

                iLo++;
            } while (iLo < iHi);

            index = iLo;
            return false;
        }

        public bool TryGetValue(AvaloniaProperty property, [MaybeNullWhen(false)] out TValue value)
        {
            if (TryGetEntry(property.Id, out var index))
            {
                value = _entries![index].Value;
                return true;
            }

            value = default;
            return false;
        }

        private void InsertEntry(Entry entry, int entryIndex)
        {
            if (_entryCount > 0)
            {
                if (_entryCount == _entries!.Length)
                {
                    const double growthFactor = 1.2;
                    var newSize = (int)(_entryCount * growthFactor);

                    if (newSize == _entryCount)
                    {
                        newSize++;
                    }

                    var destEntries = new Entry[newSize];

                    Array.Copy(_entries, 0, destEntries, 0, entryIndex);

                    destEntries[entryIndex] = entry;
                    
                    Array.Copy(_entries, entryIndex, destEntries, entryIndex + 1, _entryCount - entryIndex);
                    
                    _entries = destEntries;
                }
                else
                {
                    Array.Copy(
                        _entries,
                        entryIndex,
                        _entries,
                        entryIndex + 1,
                        _entryCount - entryIndex);

                    _entries[entryIndex] = entry;
                }
            }
            else
            {
                _entries ??= new Entry[DefaultInitialCapacity];
                _entries[0] = entry;
            }

            _entryCount++;
        }

        public void AddValue(AvaloniaProperty property, TValue value)
        {
            var propertyId = property.Id;
            TryGetEntry(propertyId, out var index);
            InsertEntry(new Entry(propertyId, value), index);
        }

        public void SetValue(AvaloniaProperty property, TValue value)
        {
            var propertyId = property.Id;
            TryGetEntry(propertyId, out var index);
            _entries![index] = new Entry(propertyId, value);
        }

        public bool Remove(AvaloniaProperty property)
        {
            if (TryGetEntry(property.Id, out var index))
            {
                Array.Copy(_entries!, index + 1, _entries!, index, _entryCount - index - 1);
                _entryCount--;
                _entries![_entryCount] = default;
                return true;
            }

            return false;
        }

        private readonly struct Entry
        {
            public readonly int PropertyId;
            public readonly TValue Value;

            public Entry(int propertyId, TValue value)
            {
                PropertyId = propertyId;
                Value = value;
            }
        }
    }
}
