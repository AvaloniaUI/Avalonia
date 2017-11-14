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
            private object _lock = new object();

            public RefCounter(IDisposable item)
            {
                _item = item;
            }

            public void AddRef()
            {
                Interlocked.Increment(ref _refs);
            }

            public void Release()
            {
                if (Interlocked.Decrement(ref _refs) == 0)
                {
                    lock (_lock)
                    {
                        _item?.Dispose();
                        _item = null;
                    }
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
                Thread.MemoryBarrier();
                _counter.AddRef();
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
                        return new Ref<T>(_item, _counter);
                    throw new ObjectDisposedException("Ref<" + typeof(T) + ">");
                }
            }

            public IRef<TResult> CloneAs<TResult>() where TResult : class
            {
                lock (_lock)
                {
                    lock (_lock)
                    {
                        if (_item != null)
                            return new Ref<TResult>((TResult) (object) _item, _counter);
                        throw new ObjectDisposedException("Ref<" + typeof(T) + ">");
                    }
                }
            }
        }
    }

}