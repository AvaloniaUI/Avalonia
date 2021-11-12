using System.Collections;
using System.Collections.Generic;
using Avalonia.Animation;

#nullable enable

namespace Avalonia.Media
{
    public class GeometryCollection : Animatable, IList<Geometry>, IReadOnlyList<Geometry>
    {
        private List<Geometry> _inner;

        public GeometryCollection() => _inner = new List<Geometry>();
        public GeometryCollection(IEnumerable<Geometry> collection) => _inner = new List<Geometry>(collection);
        public GeometryCollection(int capacity) => _inner = new List<Geometry>(capacity);

        public Geometry this[int index] 
        { 
            get => _inner[index];
            set => _inner[index] = value; 
        }

        public int Count => _inner.Count;
        public bool IsReadOnly => false;

        public void Add(Geometry item) => _inner.Add(item);
        public void Clear() => _inner.Clear();
        public bool Contains(Geometry item) => _inner.Contains(item);
        public void CopyTo(Geometry[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        public IEnumerator<Geometry> GetEnumerator() => _inner.GetEnumerator();
        public int IndexOf(Geometry item) => _inner.IndexOf(item);
        public void Insert(int index, Geometry item) => _inner.Insert(index, item);
        public bool Remove(Geometry item) => _inner.Remove(item);
        public void RemoveAt(int index) => _inner.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
