using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

internal struct RefCountingSmallDictionary<TKey> : IEnumerable<KeyValuePair<TKey, int>> where TKey : class
{
    private InlineDictionary<TKey, int> _counts;

    public bool Add(TKey key)
    {
        ref var cnt = ref _counts.GetValueRefOrAddDefault(key, out bool exists);
        cnt++;
        return !exists;
    }

    public bool Remove(TKey key)
    {
        ref var cnt = ref _counts.GetValueRefOrNullRef(key);
        cnt--;
        if (cnt == 0)
        {
            _counts.Remove(key);
            return true;
        }
        
        return false;
    }

    public InlineDictionary<TKey, int>.Enumerator GetEnumerator() => _counts.GetEnumerator();

    IEnumerator<KeyValuePair<TKey, int>> IEnumerable<KeyValuePair<TKey, int>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
