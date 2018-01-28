using System;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace Avalonia.Utilities
{
    public interface IRef<out T> : IDisposable where T : class
    {
        T Item { get; }
        IRef<T> Clone();
        IRef<TResult> CloneAs<TResult>() where TResult : class;
    }

    

    public static class RefCountable
    {
        public static IRef<T> Create<T>(T item) where T : class, IDisposable
        {
            return new Ref<T>(item, new RefCounter(item));
        }

        public static IRef<T> CreateUnownedNotClonable<T>(T item) where T : class
            => new TempRef<T>(item);

        class TempRef<T> : IRef<T> where T : class
        {
            public void Dispose()
            {
                
            }

            public TempRef(T item)
            {
                Item = item;
            }
            
            public T Item { get; }
            public IRef<T> Clone() => throw new NotSupportedException();

            public IRef<TResult> CloneAs<TResult>() where TResult : class
                => throw new NotSupportedException();
        }
        
        class RefCounter
        {
            private IDisposable _item;
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
                            _item.Dispose();
                            _item = null;
                        }
                        break;
                    }
                    old = current;
                }
            }
        }

        class Ref<T> : CriticalFinalizerObject, IRef<T> where T : class
        {
            private T _item;
            private RefCounter _counter;
            private object _lock = new object();

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
                _counter?.Release();
            }

            public T Item
            {
                get
                {
                    lock (_lock)
                    {
                        return _item;
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
        }
    }

}