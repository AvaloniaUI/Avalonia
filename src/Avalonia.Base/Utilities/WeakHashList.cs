using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;

namespace Avalonia.Utilities;

internal class WeakHashList<T> where T : class
{
    public const int DefaultArraySize = 8;
    
    private struct Key
    {
        public WeakReference<T>? Weak;
        public T? Strong;
        public int HashCode;

        public static Key MakeStrong(T r) => new()
        {
            HashCode = r.GetHashCode(),
            Strong = r
        };

        public static Key MakeWeak(T r) => new()
        {
            HashCode = r.GetHashCode(),
            Weak = new WeakReference<T>(r)
        };

        public override int GetHashCode() => HashCode;
    }

    class KeyComparer : IEqualityComparer<Key>
    {
        public bool Equals(Key x, Key y)
        {
            if (x.HashCode != y.HashCode)
                return false;
            if (x.Strong != null)
            {
                if (y.Strong != null)
                    return x.Strong == y.Strong;
                if (y.Weak == null)
                    return false;
                return y.Weak.TryGetTarget(out var weakTarget) && weakTarget == x.Strong;
            }
            else if (y.Strong != null)
            {
                if (x.Weak == null)
                    return false;
                return x.Weak.TryGetTarget(out var weakTarget) && weakTarget == y.Strong;
            }
            else
            {
                if (x.Weak == null || x.Weak.TryGetTarget(out var xTarget) == false)
                    return y.Weak?.TryGetTarget(out _) != true;
                return y.Weak?.TryGetTarget(out var yTarget) == true && xTarget == yTarget;
            }
        }

        public int GetHashCode(Key obj) => obj.HashCode;
        public static KeyComparer Instance = new();
    }

    Dictionary<Key, int>? _dic;
    WeakReference<T>?[]? _arr;
    int _arrCount;
    
    public bool IsEmpty => _dic is not null ? _dic.Count == 0 : _arrCount == 0;

    public bool NeedCompact { get; private set; }
    
    public void Add(T item)
    {
        if (_dic != null)
        {
            var strongKey = Key.MakeStrong(item);
            if (_dic.TryGetValue(strongKey, out var cnt))
                _dic[strongKey] = cnt + 1;
            else
                _dic[Key.MakeWeak(item)] = 1;
            return;
        }

        if (_arr == null)
            _arr = new WeakReference<T>[DefaultArraySize];

        if (_arrCount < _arr.Length)
        {
            _arr[_arrCount] = new WeakReference<T>(item);
            _arrCount++;
            return;
        }

        // Check if something is dead
        for (var c = 0; c < _arrCount; c++)
        {
            if (_arr[c]!.TryGetTarget(out _) == false)
            {
                _arr[c] = new WeakReference<T>(item);
                return;
            }
        }

        _dic = new Dictionary<Key, int>(KeyComparer.Instance);
        foreach (var existing in _arr)
        {
            if (existing!.TryGetTarget(out var target))
                Add(target);
        }
        
        Add(item);

        _arr = null;
        _arrCount = 0;
    }

    public void Remove(T item)
    {
        if (_arr != null)
        {
            for (var c = 0; c < _arrCount; c++)
            {
                if (_arr[c]?.TryGetTarget(out var target) == true && target == item)
                {
                    _arr[c] = null;
                    ArrCompact();
                    return;
                }
            }
        }
        else if (_dic != null)
        {
            var strongKey = Key.MakeStrong(item);
            
            if (_dic.TryGetValue(strongKey, out var cnt))
            {
                if (cnt > 1)
                {
                    _dic[strongKey] = cnt - 1;
                    return;
                }
            }

            _dic.Remove(strongKey);
        }
    }

    private void ArrCompact()
    {
        if (_arr != null)
        {
            int empty = -1;
            for (var c = 0; c < _arrCount; c++)
            {
                var r = _arr[c];
                //Mark current index as first empty
                if (r == null && empty == -1)
                    empty = c;
                //If current element isn't null and we have an empty one
                if (r != null && empty != -1)
                {
                    _arr[c] = null;
                    _arr[empty] = r;
                    empty++;
                }
            }

            if (empty != -1)
                _arrCount = empty;
        }
    }
    
    public void Compact()
    {
        if (_dic != null)
        {
            PooledList<Key>? toRemove = null;
            foreach (var kvp in _dic)
            {
                if (kvp.Key.Weak?.TryGetTarget(out _) != true)
                    (toRemove ??= new PooledList<Key>()).Add(kvp.Key);
            }

            if (toRemove != null)
            {
                foreach (var k in toRemove)
                    _dic.Remove(k);
                toRemove.Dispose();
            }
        }
    }

    private static readonly Stack<PooledList<T>> s_listPool = new();

    public static void ReturnToSharedPool(PooledList<T> list)
    {
        list.Clear();
        s_listPool.Push(list);
    }
    
    public PooledList<T>? GetAlive(Func<PooledList<T>>? factory = null)
    {
        PooledList<T>? pooled = null;
        if (_arr != null)
        {
            bool needCompact = false;
            for (var c = 0; c < _arrCount; c++)
            {
                if (_arr[c]?.TryGetTarget(out var target) == true)
                    (pooled ??= factory?.Invoke()
                                ?? (s_listPool.Count > 0
                                    ? s_listPool.Pop()
                                    : new PooledList<T>())).Add(target!);
                else
                {
                    _arr[c] = null;
                    needCompact = true;
                }
            }
            if(needCompact)
                ArrCompact();
            return pooled;
        }
        if (_dic != null)
        {
            foreach (var kvp in _dic)
            {
                if (kvp.Key.Weak?.TryGetTarget(out var target) == true)
                    (pooled ??= factory?.Invoke()
                                ?? (s_listPool.Count > 0
                                    ? s_listPool.Pop()
                                    : new PooledList<T>()))
                        .Add(target!);
                else
                    NeedCompact = true;
            }
        }

        return pooled;
    }
}
