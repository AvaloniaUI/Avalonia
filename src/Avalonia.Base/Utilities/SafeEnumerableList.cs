using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Implements a simple list which is safe to modify during enumeration.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <remarks>
    /// Implements a list which, when written to while enumerating, performs a copy of the list
    /// items. Note this this class doesn't actually implement <see cref="IList{T}"/> as it's not
    /// currently needed - feel free to add missing methods etc.
    /// </remarks>
    internal class SafeEnumerableList<T> : IEnumerable<T>
    {
        private List<T> _list = new();
        private int _generation;
        private int _enumCount = 0;

        public int Count => _list.Count;
        internal List<T> Inner => _list;

        public void Add(T item) => GetList().Add(item);
        public bool Remove(T item) => GetList().Remove(item);

        public Enumerator GetEnumerator() => new(this, _list);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        private List<T> GetList()
        {
            if (_enumCount > 0)
            {
                _list = new(_list);
                ++_generation;
                _enumCount = 0;
            }

            return _list;
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly SafeEnumerableList<T> _owner;
            private readonly List<T> _list;
            private readonly int _generation;
            private int _index;
            private T? _current;

            internal Enumerator(SafeEnumerableList<T> owner, List<T> list)
            {
                _owner = owner;
                _list = list;
                _generation = owner._generation;
                _index = 0;
                _current = default;
                ++_owner._enumCount;
            }

            public void Dispose()
            {
                if (_owner._generation == _generation)
                    --_owner._enumCount;
            }

            public bool MoveNext()
            {
                if (_index < _list.Count)
                {
                    _current = _list[_index++];
                    return true;
                }

                _current = default;
                return false;
            }

            public T Current => _current!;
            object? IEnumerator.Current => _current;

            void IEnumerator.Reset()
            {
                _index = 0;
                _current = default;
            }
        }
    }
}
