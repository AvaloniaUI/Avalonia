using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Utilities;

internal struct InlineDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : class
{
    object? _data;
    TValue? _value;

    struct KeyValuePair
    {
        public TKey? Key;
        public TValue? Value;

        public KeyValuePair(TKey key, TValue? value)
        {
            Key = key;
            Value = value;
        }

        public static implicit operator KeyValuePair<TKey?, TValue?>(KeyValuePair kvp) => new(kvp.Key, kvp.Value);
    }

    void SetCore(TKey key, TValue value, bool overwrite)
    {
        if (key == null)
            throw new ArgumentNullException();
        if (_data == null)
        {
            _data = key;
            _value = value;
        } 
        else if (_data is KeyValuePair[] arr)
        {
            var free = -1;
            for (var c = 0; c < arr.Length; c++)
            {
                if (arr[c].Key == key)
                {
                    if (overwrite)
                    {
                        arr[c] = new(key, value);
                        return;
                    }
                    else
                        throw new ArgumentException("Key already exists in dictionary");
                }

                if (arr[c].Key == null && free == -1)
                    free = c;
            }

            if (free != -1)
            {
                arr[free] = new KeyValuePair(key, value);
                return;
            }

            // Upgrade to dictionary
            var newDic = new Dictionary<TKey, TValue?>();
            foreach (var kvp in arr)
                newDic.Add(kvp.Key!, kvp.Value!);
            newDic.Add(key, value);
            _data = newDic;
        }
        else if (_data is Dictionary<TKey, TValue?> dic)
        {
            if (overwrite)
                dic[key] = value;
            else
                dic.Add(key, value);
        }
        else
        {
            // We have a single element, upgrade to array
            arr = new KeyValuePair[6];
            arr[0] = new KeyValuePair((TKey)_data, _value);
            arr[1] = new KeyValuePair(key, value);
            _data = arr;
            _value = default;
        }
    }

    public void Add(TKey key, TValue value) => SetCore(key, value, false);
    public void Set(TKey key, TValue value) => SetCore(key, value, true);

    public TValue this[TKey key]
    {
        get
        {
            if (TryGetValue(key, out var rv))
                return rv;
            throw new KeyNotFoundException();
        }
        set => Set(key, value);
    }

    public bool Remove(TKey key)
    {
        if (_data == key)
        {
            _data = null;
            _value = default;
            return true;
        } 
        else if (_data is KeyValuePair[] arr)
        {
            for (var c = 0; c < arr.Length; c++)
            {
                if (arr[c].Key == key)
                {
                    arr[c] = default;
                    return true;
                }
            }

            return false;
        }
        else if (_data is Dictionary<TKey, TValue?> dic)
            return dic.Remove(key);

        return false;
    }

    public bool HasEntries => _data != null;
    
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)]out TValue value)
    {
        if (_data == key)
        {
            value = _value!;
            return true;
        } 
        else if (_data is KeyValuePair[] arr)
        {
            for (var c = 0; c < arr.Length; c++)
            {
                if (arr[c].Key == key)
                {
                    value = arr[c].Value!;
                    return true;
                }
            }

            value = default;
            return false;
        }
        else if (_data is Dictionary<TKey, TValue?> dic)
            return dic.TryGetValue(key, out value);

        value = default;
        return false;
    }

#if NET6_0_OR_GREATER
    [UnscopedRef]
    public ref TValue GetValueRefOrNullRef(TKey key)
    {
        if (_data == key)
        {
            return ref (_value!);
        } 
        else if (_data is KeyValuePair[] arr)
        {
            for (var c = 0; c < arr.Length; c++)
            {
                if (arr[c].Key == key)
                    return ref arr[c].Value!;
            }

            return ref Unsafe.NullRef<TValue>();
        }
        if (_data is Dictionary<TKey, TValue?> dic)
            return ref CollectionsMarshal.GetValueRefOrNullRef(dic, key)!;
        return ref Unsafe.NullRef<TValue>();
    }
    
    [UnscopedRef]
    public ref TValue GetValueRefOrAddDefault(TKey key, [UnscopedRef] out bool exists)
    {
        if (_data == null)
        {
            exists = false;
            _data = key;
            return ref _value!;
        }
        
        // Single element
        if (_data == key)
        {
            exists = true;
            return ref (_value!);
        }

        // Small array
        if (_data is KeyValuePair[] arr)
        {
            // Try to find the element and look for the first free slot while we are at it
            int free = -1;
            for (var c = 0; c < arr.Length; c++)
            {
                if (arr[c].Key == key)
                {
                    exists = true;
                    return ref arr[c].Value!;
                }
                else if (free == -1 && arr[c].Key == null)
                    free = c;
            }

            // There is a free slot, use it
            if (free != -1)
            {
                arr[free] = new(key, default);
                exists = false;
                return ref arr[free].Value!;
            }
            
            // Upgrade to dictionary
            var newDic = new Dictionary<TKey, TValue?>();
            foreach (var kvp in arr)
                newDic.Add(kvp.Key!, kvp.Value!);
            _data = newDic;
            return ref CollectionsMarshal.GetValueRefOrAddDefault(newDic, key, out exists)!;
        }
        
        if (_data is Dictionary<TKey, TValue?> dic)
            return ref CollectionsMarshal.GetValueRefOrAddDefault(dic, key, out exists)!;

        // This is a second element, convert to array 
        // We have a single element, upgrade to array
        arr = new KeyValuePair[6];
        arr[0] = new KeyValuePair((TKey)_data, _value);
        arr[1] = new KeyValuePair(key, default);
        _data = arr;
        _value = default;
        exists = false;
        return ref arr[1].Value!;

    }
    
#endif

    public bool TryGetAndRemoveValue(TKey key, [MaybeNullWhen(false)]out TValue value)
    {
        if (_data == key)
        {
            value = _value!;
            _value = default;
            _data = null;
            return true;
        } 
        else if (_data is KeyValuePair[] arr)
        {
            for (var c = 0; c < arr.Length; c++)
            {
                if (arr[c].Key == key)
                {
                    value = arr[c].Value!;
                    arr[c] = default;
                    return true;
                }
            }

            value = default;
            return false;
        }
        else if (_data is Dictionary<TKey, TValue?> dic)
        {
            if (!dic.TryGetValue(key, out value))
                return false;
            dic.Remove(key);
        }

        value = default;
        return false;
    }

    public TValue GetAndRemove(TKey key)
    {
        if (TryGetAndRemoveValue(key, out var v))
            return v;
        throw new KeyNotFoundException();
    }

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private Dictionary<TKey, TValue>.Enumerator _inner;
        private readonly KeyValuePair[]? _arr;
        private KeyValuePair<TKey, TValue> _first;
        private int _index;
        private readonly Type _type;
        enum Type
        {
            Empty, Single, Array, Dictionary
        }

        public Enumerator(InlineDictionary<TKey, TValue> parent)
        {
            _arr = null;
            _first = default;
            _index = -1;
            _inner = default;
            if (parent._data is Dictionary<TKey, TValue> inner)
            {
                _inner = inner.GetEnumerator();
                _type = Type.Dictionary;
            }
            else if (parent._data is KeyValuePair[] arr)
            {
                _type = Type.Array;
                _arr = arr;
            }
            else if (parent._data != null)
            {
                _type = Type.Single;
                _first = new((TKey)parent._data!, parent._value!);
            }
            else
                _type = Type.Empty;

        }

        public bool MoveNext()
        {
            if (_type == Type.Single)
            {
                if (_index != -1)
                    return false;
                _index = 0;
                return true;
            }
            else if (_type == Type.Array)
            {
                for (var next = _index + 1; next < _arr!.Length; ++next)
                {
                    if (_arr[next].Key != null)
                    {
                        _index = next;
                        return true;
                    }
                }
                return false;
            }
            else if (_type == Type.Dictionary)
                return _inner.MoveNext();

            return false;
        }

        public void Reset()
        {
            _index = -1;
            if(_type == Type.Dictionary)
                ((IEnumerator)_inner).Reset();
        }

        public KeyValuePair<TKey, TValue> Current
        {
            get
            {
                if (_type == Type.Single)
                    return _first!;
                if (_type == Type.Array)
                    return _arr![_index]!;
                if (_type == Type.Dictionary)
                    return _inner.Current;
                throw new InvalidOperationException();
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}
