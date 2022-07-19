using System.Collections.Generic;

namespace Avalonia.PropertyStore
{
    internal static class DictionaryPool<TKey, TValue>
        where TKey : notnull
    {
        private const int MaxPoolSize = 4;
        private static Stack<Dictionary<TKey, TValue>> _pool = new();

        public static Dictionary<TKey, TValue> Get()
        {
            return _pool.Count == 0 ? new() : _pool.Pop();
        }

        public static void Release(Dictionary<TKey, TValue> dictionary)
        {
            if (_pool.Count < MaxPoolSize)
            {
                dictionary.Clear();
                _pool.Push(dictionary);
            }
        }
    }
}
