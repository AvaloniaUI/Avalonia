using System.Collections.Generic;

namespace Avalonia.PropertyStore
{
#if NETSTANDARD2_0
    internal static class CollectionExtensions
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
#endif
}
