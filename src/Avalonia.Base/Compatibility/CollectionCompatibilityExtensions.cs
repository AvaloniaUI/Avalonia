using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System;

#if !NET6_0_OR_GREATER
internal static class CollectionCompatibilityExtensions
{
    public static bool Remove<TKey, TValue>(
        this Dictionary<TKey, TValue> o,
        TKey key,
        [MaybeNullWhen(false)] out TValue value)
            where TKey : notnull
    {
        if (o.TryGetValue(key, out value))
            return o.Remove(key);
        return false;
    }

    public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> o, TKey key, TValue value)
        where TKey : notnull
    {
        if (!o.ContainsKey(key))
        {
            o.Add(key, value);
            return true;
        }

        return false;
    }
}
#endif
