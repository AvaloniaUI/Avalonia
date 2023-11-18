using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Utilities;

internal struct PooledInlineList<T> : IDisposable, IEnumerable<T> where T : class
{
    private object? _item;

    public PooledInlineList()
    {
        
    }
    
    public void Add(T item)
    {
        if (_item == null)
            _item = item;
        else if (_item is SimplePooledList list)
            list.Add(item);
        else
        {
            ConvertToList();
            Add(item);
        }
    }
    
    public bool Remove(T item)
    {
        if (item == _item)
        {
            _item = null;
            return true;
        }

        if (item is SimplePooledList list)
            return list.Remove(item);

        return false;
    }

    void ConvertToList()
    {
        if (_item is SimplePooledList)
            return;
        var list = new SimplePooledList();
        if (_item != null)
            list.Add((T)_item);
        _item = list;
    }

    public void EnsureCapacity(int count)
    {
        if (count < 2)
            return;
        ConvertToList();
        ((SimplePooledList)_item!).EnsureCapacity(count);
    }

    public void Dispose()
    {
        if (_item is SimplePooledList list)
            list.Dispose();
        _item = null;
    }

    public int Count => _item == null ? 0 : _item is SimplePooledList list ? list.Count : 1;
    
    class SimplePooledList : IDisposable
    {
        public int Count;
        public T[]? Items;

        public void Add(T item)
        {
            if (Items == null) 
                Items = ArrayPool<T>.Shared.Rent(4);
            else if (Count == Items.Length) 
                GrowItems(Count * 2);

            Items[Count] = item;
            Count++;
        }
        
        private void ReturnToPool(T[] items)
        {
#if NETCOREAPP2_1_OR_GREATER
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#endif
            {
                Array.Clear(items, 0, Count);
            }
            ArrayPool<T>.Shared.Return(items);
        }

        void GrowItems(int count)
        {
            if (count < Count)
                return;
            var newArr = ArrayPool<T>.Shared.Rent(count);
            Array.Copy(Items!, newArr, Count);
            ReturnToPool(Items!);
            Items = newArr;
        }
        
        public void EnsureCapacity(int count)
        {
            if (Items == null)
                Items = ArrayPool<T>.Shared.Rent(count);
            else if (Items.Length < count) GrowItems(count);
        }

        public void Dispose()
        {
            if(Items == null)
                return;

            ReturnToPool(Items);
            
            Items = null!;
            Count = 0;
        }

        public bool Remove(T item)
        {
            for (var c = 0; c < Count; c++)
            {
                if (item == Items![c])
                {
                    Items[c] = null!;
                    Count--;
                    if (c < Count)
                        Array.Copy(Items, c + 1, Items, c, Count - c);
                    return true;
                }
            }

            return false;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    
    public Enumerator GetEnumerator() => new(_item);
    
    public struct Enumerator : IEnumerator<T>
    {
        private readonly T? _singleItem;
        private int _index;
        private readonly SimplePooledList? _list;
        
        public Enumerator(object? item)
        {
            _index = -1;
            _list = item as SimplePooledList;
            if (_list == null)
                _singleItem = (T?)item;
        }

        public bool MoveNext()
        {
            if (_singleItem != null)
            {
                if (_index >= 0)
                    return false;
                _index = 0;
                return true;
            }

            if (_list != null)
            {
                if (_index >= _list.Count - 1)
                    return false;
                _index++;
                return true;
            }

            return false;
        }

        public void Reset() => throw new NotSupportedException();
        object IEnumerator.Current => Current;

        public T Current
        {
            get
            {
                if (_list != null)
                    return _list.Items![_index];
                return _singleItem!;
            }
        }

        public void Dispose()
        {
        }
    }
    
    /// <summary>
    ///  For compositor serialization purposes only, takes the ownership of previously transferred state
    /// </summary>
    public PooledInlineList(object? rawState)
    {
        _item = rawState;
    }

    /// <summary>
    ///  For compositor serialization purposes only, gives up the ownership of the internal state and returns it
    /// </summary>
    public object? TransferRawState()
    {
        var rv = _item;
        _item = null;
        return rv;
    }
}