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
        private Entry[]? _entries;
        private int _entryCount;

        public AvaloniaPropertyValueStore()
        {
            _entries = null;
            _entryCount = 0;
            IsInitializing = false;
            InitialSize = 4;
        }

        public int Count => _entryCount;
        
        public bool IsInitializing { get; set; }

        public int InitialSize { get; set; }

        public TValue this[int index] => _entries![index].Value;

        private EntryIndex LookupEntry(int propertyId)
        {
            int checkIndex;
            int iLo = 0;
            int iHi = _entryCount;

            if (iHi <= 0)
            {
                return new EntryIndex(0, found: false);
            }

            // Do a binary search to find the value
            while (iHi - iLo > 3)
            {
                int iPv = (iHi + iLo) / 2;
                checkIndex = _entries![iPv].PropertyId;

                if (propertyId == checkIndex)
                {
                    return new EntryIndex(iPv, found: true);
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
                    return new EntryIndex(iLo, found: true);
                }

                if (checkIndex > propertyId)
                {
                    // we've gone past the targetIndex - return not found
                    break;
                }

                iLo++;
            } while (iLo < iHi);

            return new EntryIndex(iLo, found: false);
        }

        public bool TryGetValue(AvaloniaProperty property, [MaybeNullWhen(false)] out TValue value)
        {
            var entryIndex = LookupEntry(property.Id);
            
            if (!entryIndex.Found)
            {
                value = default;
                return false;
            }

            value = _entries![entryIndex.Index].Value;
            
            return true;
        }

        private void InsertEntry(Entry entry, int entryIndex)
        {
            if (_entryCount > 0)
            {
                if (_entryCount == _entries!.Length)
                {
                    // We want to have more aggressive resizing when initializing.
                    var growthFactor = IsInitializing ? 2.0 : 1.2;
                    
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
                if (_entries is null)
                {
                    _entries = new Entry[InitialSize];
                }
                
                _entries[0] = entry;
            }

            _entryCount++;
        }

        public void AddValue(AvaloniaProperty property, TValue value)
        {
            var propertyId = property.Id;
            var index = LookupEntry(propertyId);

            InsertEntry(new Entry(propertyId, value), index.Index);
        }

        public void SetValue(AvaloniaProperty property, TValue value)
        {
            var propertyId = property.Id;
            var entryIndex = LookupEntry(propertyId);
            
            _entries![entryIndex.Index] = new Entry(propertyId, value);
        }

        public void Remove(AvaloniaProperty property)
        {
            var entry = LookupEntry(property.Id);

            if (!entry.Found) return;
            
            Array.Copy(_entries!, entry.Index + 1, _entries!, entry.Index, _entryCount - entry.Index - 1);

            _entryCount--;
            _entries![_entryCount] = default;
        }

        private readonly struct EntryIndex
        {
            public readonly int Index;
            public readonly bool Found;

            public EntryIndex(int index, bool found)
            {
                Index = index;
                Found = found;
            }
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
