using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Stores values with <see cref="AvaloniaProperty"/> as key.
    /// </summary>
    /// <typeparam name="TValue">Stored value type.</typeparam>
    /// <remarks>
    /// This struct implements the most commonly-used part of the dictionary API, but does
    /// not implement <see cref="IDictionary{TKey, TValue}"/>. In particular, this struct
    /// is not enumerable. Enumeration is intended to be done by index for better performance.
    /// </remarks>
    internal struct AvaloniaPropertyDictionary<TValue>
    {
        private const int DefaultInitialCapacity = 4;
        private Entry[]? _entries;
        private int _entryCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaPropertyDictionary{TValue}"/>
        /// class that is empty and has the default initial capacity.
        /// </summary>
        public AvaloniaPropertyDictionary()
        {
            _entries = null;
            _entryCount = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaPropertyDictionary{TValue}"/>
        /// class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capactity">
        /// The initial number of elements that the collection can contain.
        /// </param>
        public AvaloniaPropertyDictionary(int capactity)
        {
            _entries = new Entry[capactity];
            _entryCount = 0;
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the collection.
        /// </summary>
        public int Count => _entryCount;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="property">The key to get or set.</param>
        /// <returns>
        /// The value associated with the specified key. If the key is not found, a get operation
        /// throws a <see cref="KeyNotFoundException"/>, and a set operation creates a
        /// new element for the specified key.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// The key does not exist in the collection.
        /// </exception>
        public TValue this[AvaloniaProperty property]
        {
            get
            {
                var index = FindEntry(property.Id);
                if (index < 0)
                    ThrowNotFound();
                return UnsafeGetEntryRef(index).Value;
            }
            set
            {
                var index = FindEntry(property.Id);
                if (index >= 0)
                    UnsafeGetEntryRef(index) = new Entry(property, value);
                else
                    InsertEntry(new Entry(property, value), ~index);
            }
        }

        /// <summary>
        /// Gets the value at the specified index.
        /// </summary>
        /// <param name="index">
        /// The index of the entry, between 0 and <see cref="Count"/> - 1.
        /// </param>
        public TValue this[int index]
        {
            get
            {
                if (index >= _entryCount)
                    ThrowOutOfRange();
                return UnsafeGetEntryRef(index).Value;
            }
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="property">The key.</param>
        /// <param name="value">The value of the element to add.</param>
        public void Add(AvaloniaProperty property, TValue value)
        {
            var index = FindEntry(property.Id);
            if (index >= 0)
                ThrowDuplicate();
            InsertEntry(new Entry(property, value), ~index);
        }

        /// <summary>
        /// Removes all keys and values from the collection.
        /// </summary>
        /// <remarks>
        /// The Count property is set to 0, and references to other objects from elements of the
        /// collection are also released. The capacity remains unchanged.
        /// </remarks>
        public void Clear()
        {
            if (_entries is not null)
            {
                Array.Clear(_entries, 0, _entries.Length);
                _entryCount = 0;
            }
        }

        /// <summary>
        /// Determines whether the collection contains the specified key.
        /// </summary>
        /// <param name="property">The key.</param>
        public bool ContainsKey(AvaloniaProperty property) => FindEntry(property.Id) >= 0;

        /// <summary>
        /// Gets value at the specified index.
        /// </summary>
        /// <param name="index">The index of the entry, between 0 and <see cref="Count"/> - 1.</param>
        /// <returns>The value at the specified index.</returns>
        public TValue GetValue(int index)
        {
            if (index >= _entryCount)
                ThrowOutOfRange();
            ref var entry = ref UnsafeGetEntryRef(index);
            return entry.Value;
        }

        /// <summary>
        /// Removes the value of the specified key from the collection.
        /// </summary>
        /// <param name="property">The key.</param>
        /// <returns>
        /// true if the element is successfully found and removed; otherwise, false. This method
        /// returns false if key is not found in the collection.
        /// </returns>
        public bool Remove(AvaloniaProperty property)
        {
            var index = FindEntry(property.Id);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the value of the specified key from the collection, and copies the element to
        /// the value parameter.
        /// </summary>
        /// <param name="property">The key.</param>
        /// <param name="value">The removed element.</param>
        /// <returns>
        /// true if the element is successfully found and removed; otherwise, false. This method
        /// returns false if key is not found in the collection.
        /// </returns>
        public bool Remove(AvaloniaProperty property, [MaybeNullWhen(false)] out TValue value)
        {
            var index = FindEntry(property.Id);
            if (index >= 0)
            {
                value = UnsafeGetEntryRef(index).Value;
                RemoveAt(index);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Removes the element at the specified index from the collection.
        /// </summary>
        /// <param name="index">The index.</param>
        public void RemoveAt(int index)
        {
            if (_entries is null)
                ThrowOutOfRange();

            Array.Copy(_entries, index + 1, _entries, index, _entryCount - index - 1);
            _entryCount--;
            UnsafeGetEntryRef(_entryCount) = default;
        }

        /// <summary>
        /// Attempts to add the specified key and value to the collection.
        /// </summary>
        /// <param name="property">The key.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns></returns>
        public bool TryAdd(AvaloniaProperty property, TValue value)
        {
            var index = FindEntry(property.Id);
            if (index >= 0)
                return false;
            InsertEntry(new Entry(property, value), ~index);
            return true;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="property">The property key.</param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified key,
        /// if the property is found; otherwise, null. This parameter is passed uninitialized.
        /// </param>
        /// <returns></returns>
        public bool TryGetValue(AvaloniaProperty property, [MaybeNullWhen(false)] out TValue value)
        {
            // <!> Very performance critical code: FindEntry has been manually inlined here.
            // This gives a ~20% speedup in micro-benchmarks.

            var lo = 0;
            var hi = _entryCount - 1;

            if (hi >= 0)
            {
                var propertyId = property.Id;
                ref var entry0 = ref UnsafeGetEntryRef(0);

                do
                {
                    // hi and lo are never negative: there's no overflow using unsigned math
                    var i = (int)(((uint)hi + (uint)lo) >> 1);

#if NET6_0_OR_GREATER
                    // nuint cast to force zero extend instead of sign extend
                    ref var entry = ref Unsafe.Add(ref entry0, (nuint)i);
#else
                    ref var entry = ref Unsafe.Add(ref entry0, i);
#endif

                    var entryId = entry.Id;
                    if (entryId == propertyId)
                    {
                        value = entry.Value;
                        return true;
                    }

                    if (entryId < propertyId)
                    {
                        lo = i + 1;
                    }
                    else
                    {
                        hi = i - 1;
                    }
                } while (lo <= hi);
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(int propertyId)
        {
            var lo = 0;
            var hi = _entryCount - 1;

            if (hi >= 0)
            {
                ref var entry0 = ref UnsafeGetEntryRef(0);

                do
                {
                    // hi and lo are never negative: there's no overflow using unsigned math
                    var i = (int)(((uint)hi + (uint)lo) >> 1);

#if NET6_0_OR_GREATER
                    // nuint cast to force zero extend instead of sign extend
                    ref var entry = ref Unsafe.Add(ref entry0, (nuint)i);
#else
                    ref var entry = ref Unsafe.Add(ref entry0, i);
#endif

                    var entryId = entry.Id;
                    if (entryId == propertyId)
                    {
                        return i;
                    }

                    if (entryId < propertyId)
                    {
                        lo = i + 1;
                    }
                    else
                    {
                        hi = i - 1;
                    }
                } while (lo <= hi);
            }

            return ~lo;
        }

        [MemberNotNull(nameof(_entries))]
        private void InsertEntry(Entry entry, int entryIndex)
        {
            if (_entryCount > 0)
            {
                if (_entryCount == _entries!.Length)
                {
                    var newSize = _entryCount == DefaultInitialCapacity ?
                        DefaultInitialCapacity * 2 :
                        (int)(_entryCount * 1.5);

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

                    UnsafeGetEntryRef(entryIndex) = entry;
                }
            }
            else
            {
                _entries ??= new Entry[DefaultInitialCapacity];
                UnsafeGetEntryRef(0) = entry;
            }

            _entryCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref Entry UnsafeGetEntryRef(int index)
        {
#if NET6_0_OR_GREATER && !DEBUG
            // This type is performance critical: in release mode, skip any bound check the JIT compiler couldn't elide.
            // The index parameter should always be correct when calling this method: no unchecked user input should get here.
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries!), (uint)index);
#else
            return ref _entries![index];
#endif
        }

        [DoesNotReturn]
        private static void ThrowOutOfRange() => throw new IndexOutOfRangeException();

        [DoesNotReturn]
        private static void ThrowDuplicate() => 
            throw new ArgumentException("An item with the same key has already been added.");

        [DoesNotReturn]
        private static void ThrowNotFound() => throw new KeyNotFoundException();

        private readonly struct Entry
        {
            public readonly int Id;
            public readonly TValue Value;

            public Entry(AvaloniaProperty property, TValue value)
            {
                Id = property.Id;
                Value = value;
            }
        }
    }
}
