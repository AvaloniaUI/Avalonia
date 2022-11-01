using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.PropertyStore
{
    internal static class AvaloniaPropertyDictionaryPool<TValue>
    {
        private const int MaxPoolSize = 4;
        private static readonly Stack<AvaloniaPropertyDictionary<TValue>> _pool = new();

        public static AvaloniaPropertyDictionary<TValue> Get()
        {
            return _pool.Count == 0 ? new() : _pool.Pop();
        }

        public static void Release(AvaloniaPropertyDictionary<TValue> dictionary)
        {
            if (_pool.Count < MaxPoolSize)
            {
                dictionary.Clear();
                _pool.Push(dictionary);
            }
        }
    }
}
