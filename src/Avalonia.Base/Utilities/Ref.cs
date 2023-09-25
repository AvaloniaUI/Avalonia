using System;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace Avalonia.Utilities
{
    /// <summary>
    /// A ref-counted wrapper for a disposable object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IRef<out T> : IDisposable where T : class
    {
        /// <summary>
        /// The item that is being ref-counted.
        /// </summary>
        T Item { get; }

        /// <summary>
        /// Create another reference to this object and increment the refcount.
        /// </summary>
        /// <returns>A new reference to this object.</returns>
        IRef<T> Clone();

        /// <summary>
        /// Create another reference to the same object, but cast the object to a different type.
        /// </summary>
        /// <typeparam name="TResult">The type of the new reference.</typeparam>
        /// <returns>A reference to the value as the new type but sharing the refcount.</returns>
        IRef<TResult> CloneAs<TResult>() where TResult : class;


        /// <summary>
        /// The current refcount of the object tracked in this reference. For debugging/unit test use only.
        /// </summary>
        int RefCount { get; }
    }

    

    internal static class RefCountable
    {
        /// <summary>
        /// Create a reference counted object wrapping the given item.
        /// </summary>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <param name="item">The item to refcount.</param>
        /// <returns>The refcounted reference to the item.</returns>
        public static IRef<T> Create<T>(T item) where T : class, IDisposable
        {
            return new Ref<T>(item, new RefCounter(item));
        }

        class RefCounter
        {
            private IDisposable? _item;
            private volatile int _refs;

            public RefCounter(IDisposable item)
            {
                _item = item;
                _refs = 1;
            }

            public void AddRef()
            {
                var old = _refs;
                while (true)
                {
                    if (old == 0)
                    {
                        throw new ObjectDisposedException("Cannot add a reference to a nonreferenced item");
                    }
                    var current = Interlocked.CompareExchange(ref _refs, old + 1, old);
                    if (current == old)
                    {
                        break;
                    }
                    old = current;
                }
            }

            public void Release()
            {
                var old = _refs;
                while (true)
                {
                    var current = Interlocked.CompareExchange(ref _refs, old - 1, old);

                    if (current == old)
                    {
                        if (old == 1)
                        {
                            _item?.Dispose();
                            _item = null;
                        }
                        break;
                    }
                    old = current;
                }
            }

            internal int RefCount => _refs;
        }

        class Ref<T> : CriticalFinalizerObject, IRef<T> where T : class
        {
            private T? _item;
            private readonly RefCounter _counter;
            private readonly object _lock = new object();

            public Ref(T item, RefCounter counter)
            {
                _item = item;
                _counter = counter;
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    if (_item != null)
                    {
                        _counter.Release();
                        _item = null;
                    }
                    GC.SuppressFinalize(this);
                }
            }

            ~Ref()
            {
                Dispose();
            }

            public T Item
            {
                get
                {
                    lock (_lock)
                    {
                        return _item!;
                    }
                }
            }

            public IRef<T> Clone()
            {
                lock (_lock)
                {
                    if (_item != null)
                    {
                        var newRef = new Ref<T>(_item, _counter);
                        _counter.AddRef();
                        return newRef;
                    }
                    throw new ObjectDisposedException("Ref<" + typeof(T) + ">");
                }
            }

            public IRef<TResult> CloneAs<TResult>() where TResult : class
            {
                lock (_lock)
                {
                    if (_item != null)
                    {
                        var castRef = new Ref<TResult>((TResult)(object)_item, _counter);
                        Interlocked.MemoryBarrier();
                        _counter.AddRef();
                        return castRef;
                    }
                    throw new ObjectDisposedException("Ref<" + typeof(T) + ">");
                }
            }

            public int RefCount => _counter.RefCount;
        }
    }

}
