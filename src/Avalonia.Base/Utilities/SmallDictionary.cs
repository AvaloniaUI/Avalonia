using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Utilities;

public struct InlineDictionary<TKey, TValue> where TKey : class where TValue : class
{
    object? _data;
    TValue? _value;

    void SetCore(TKey key, TValue value, bool overwrite)
    {
        if (_data == null)
        {
            _data = key;
            _value = value;
        } 
        else if (_data is KeyValuePair<TKey?, TValue?>[] arr)
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

                if (arr[c].Key == null)
                    free = c;
            }

            if (free != -1)
            {
                arr[free] = new KeyValuePair<TKey?, TValue?>(key, value);
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
            arr = new KeyValuePair<TKey?, TValue?>[6];
            arr[0] = new KeyValuePair<TKey?, TValue?>((TKey)_data, _value);
            arr[1] = new KeyValuePair<TKey?, TValue?>(key, value);
            _data = arr;
            _value = null;
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
            _value = null;
            return true;
        } 
        else if (_data is KeyValuePair<TKey?, TValue?>[] arr)
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

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)]out TValue value)
    {
        if (_data == key)
        {
            value = _value!;
            return true;
        } 
        else if (_data is KeyValuePair<TKey?, TValue?>[] arr)
        {
            for (var c = 0; c < arr.Length; c++)
            {
                if (arr[c].Key == key)
                {
                    value = arr[c].Value!;
                    return true;
                }
            }

            value = null;
            return false;
        }
        else if (_data is Dictionary<TKey, TValue?> dic)
            return dic.TryGetValue(key, out value);

        value = null;
        return false;
    }
    
    
    public bool TryGetAndRemoveValue(TKey key, [MaybeNullWhen(false)]out TValue value)
    {
        if (_data == key)
        {
            value = _value!;
            _value = null;
            _data = null;
            return true;
        } 
        else if (_data is KeyValuePair<TKey?, TValue?>[] arr)
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

            value = null;
            return false;
        }
        else if (_data is Dictionary<TKey, TValue?> dic)
        {
            if (!dic.TryGetValue(key, out value))
                return false;
            dic.Remove(key);
        }

        value = null;
        return false;
    }

    public TValue GetAndRemove(TKey key)
    {
        if (TryGetAndRemoveValue(key, out var v))
            return v;
        throw new KeyNotFoundException();
    }
}