using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Utilities
{
    public struct ImmutableReadOnlyListStructEnumerator<T> : IEnumerator, IEnumerator<T>
    {
        private readonly IReadOnlyList<T> _readOnlyList;
        private int _pos;

        public ImmutableReadOnlyListStructEnumerator(IReadOnlyList<T> readOnlyList)
        {
            _readOnlyList = readOnlyList;
            _pos = -1;
            Current = default;
        }

        public T Current
        {
            get;
            private set;
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            if (_pos >= _readOnlyList.Count - 1)
            {
                return false;
            }

            Current = _readOnlyList[++_pos];

            return true;

        }

        public void Reset()
        {
            _pos = -1;

            Current = default;
        }
    }
}
