using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

#nullable enable

namespace Avalonia.Media
{
    /// <summary>
    /// A unordered collection of weak references to objects of type <typeparamref name="T"/>. Specialised for use with <see cref="MediaInvalidation"/>.
    /// </summary>
    /// <seealso cref="Utilities.WeakHashList{T}"/>
    [DebuggerDisplay("Count upper bound = {CountUpperBound}")]
    internal class MediaParentsBag<T> : IEnumerable<T> where T : class
    {
        /// <summary>
        /// This dictionary exists only to accelerate <see cref="Remove(T)"/>. An item can be added to the same bag any number of times.
        /// </summary>
        private Dictionary<int, List<GCHandle>>? _handleBuckets;

        /// <summary>
        /// The most common case is for an object to only ever have one parent, which is never removed.
        /// </summary>
        private (int hash, GCHandle handle)? _singleParent;

        public int CountUpperBound => _singleParent.HasValue ? 1 : _handleBuckets?.Values.Sum(l => l.Count) ?? 0;

        ~MediaParentsBag()
        {
            if (_singleParent.HasValue)
            {
                _singleParent.Value.handle.Free();
            }
            else if (_handleBuckets != null)
            {
                foreach (var handle in _handleBuckets.Values.SelectMany(l => l))
                {
                    handle.Free();
                }
            }
        }

        public void Add(T item)
        {
            var handle = GCHandle.Alloc(item, GCHandleType.Weak);
            var hashCode = item.GetHashCode();

            if (_handleBuckets == null)
            {
                if (_singleParent == null) // first item
                {
                    _singleParent = (hashCode, handle);
                }
                else // second item
                {
                    _handleBuckets = new()
                    {
                        { _singleParent.Value.hash, new(1) { _singleParent.Value.handle } },
                        { hashCode , new(1) { handle } },
                    };
                    _singleParent = null;
                }
            }
            else // third and subsequent items
            {
                if (_handleBuckets.TryGetValue(hashCode, out var parentReferences))
                {
                    parentReferences.Add(handle);
                }
                else
                {
                    _handleBuckets[hashCode] = new(1) { handle };
                }
            }
        }

        public bool Remove(T item)
        {
            if (_singleParent.HasValue)
            {
                if (ReferenceEquals(_singleParent.Value.handle.Target, item))
                {
                    var handle = _singleParent.Value.handle;
                    _singleParent = null;
                    handle.Free();
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (_handleBuckets == null)
            {
                return false;
            }

            var hashCode = item.GetHashCode();
            if (!_handleBuckets.TryGetValue(hashCode, out var list))
            {
                return false;
            }

            int i = 0;
            var removed = false;
            foreach (var parent in EnumerateAndTrimHandles(list))
            {
                if (ReferenceEquals(parent, item))
                {
                    var handle = list[i];
                    list.RemoveAt(i);
                    handle.Free();

                    removed = true;
                    break;
                }
                i++;
            }

            if (list.Count == 0)
            {
                _handleBuckets.Remove(hashCode);
            }

            return removed;
        }

        private IEnumerable<T> EnumerateAndTrimHandles(List<GCHandle> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var target = list[i].Target; // create a strong reference

                while (target == null)
                {
                    var staleHandle = list[i];
                    list.RemoveAt(i);
                    staleHandle.Free();

                    if (i >= list.Count)
                    {
                        yield break;
                    }

                    target = list[i].Target;
                }

                yield return (T)target;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_singleParent.HasValue)
            {
                var singleTarget = _singleParent.Value.handle.Target;
                if (singleTarget != null)
                {
                    yield return (T)singleTarget;
                }
            }
            else if (_handleBuckets != null)
            {
                foreach (var target in _handleBuckets.Values.SelectMany(EnumerateAndTrimHandles))
                {
                    yield return target;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

