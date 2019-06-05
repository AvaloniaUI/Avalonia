using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Utilities
{
    public class WeakList<T> : IEnumerable<T> where T : class 
    {
        private readonly List<WeakReference<T>> _inner = new List<WeakReference<T>>();
        private readonly List<WeakReference<T>> _toRemove = new List<WeakReference<T>>();

        public void Add(T item)
        {
            _inner.Add(new WeakReference<T>(item));
        }


        public int SupposedCount => _inner.Count;

        public void Remove(T item)
        {
            for(var c=0; c<_inner.Count; c++)
                if (_inner[c].TryGetTarget(out var target) && target == item)
                {
                    _inner.RemoveAt(c);
                    return;
                }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var reference in _inner)
            {
                if(reference.TryGetTarget(out var target) && target != null)
                    yield return target;
                else
                    _toRemove.Add(reference);
            }
            for (var c = 0; c < _toRemove.Count; c++)
                _inner.Remove(_toRemove[c]);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
