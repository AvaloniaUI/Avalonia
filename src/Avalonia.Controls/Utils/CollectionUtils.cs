using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls.Utils
{
    internal static class CollectionUtils
    {
        public static NotifyCollectionChangedEventArgs ResetEventArgs { get; } = new(NotifyCollectionChangedAction.Reset);

        public static void InsertMany<T>(this List<T> list, int index, T item, int count)
        {
            var repeat = FastRepeat<T>.Instance;
            repeat.Count = count;
            repeat.Item = item;
            list.InsertRange(index, FastRepeat<T>.Instance);
            repeat.Item = default;
        }

        private class FastRepeat<T> : ICollection<T>
        {
            public static readonly FastRepeat<T> Instance = new();
            public int Count { get; set; }
            public bool IsReadOnly => true;
            [AllowNull] public T Item { get; set; }
            public void Add(T item) => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(T item) => throw new NotImplementedException();
            public bool Remove(T item) => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
            public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();

            public void CopyTo(T[] array, int arrayIndex)
            {
                var end = arrayIndex + Count;

                for (var i = arrayIndex; i < end; ++i)
                {
                    array[i] = Item;
                }
            }
        }
    }
}
