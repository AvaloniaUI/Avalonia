using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if !NET6_0_OR_GREATER
namespace System
{
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
    
    namespace Runtime.InteropServices
    {
        public static class CollectionsMarshal
        {
            public static Span<T> AsSpan<T>(List<T>? list)
                => list is null ? default : new Span<T>(Unsafe.As<StrongBox<T[]>>(list).Value, 0, list.Count);
        }
    }
}

#endif
