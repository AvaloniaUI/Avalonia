﻿using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Threading;

namespace Avalonia.Reactive
{
    /// <summary>
    /// Lightweight base class for observable implementations.
    /// </summary>
    /// <typeparam name="T">The observable type.</typeparam>
    /// <remarks>
    /// <see cref="ObservableBase{T}"/> is rather heavyweight in terms of allocations and memory
    /// usage. This class provides a more lightweight base for some internal observable types
    /// in the Avalonia framework.
    /// </remarks>
    public abstract class LightweightObservableBase<T> : IObservable<T>
    {
        private Exception _error;
        private List<IObserver<T>> _observers = new List<IObserver<T>>();

        public IDisposable Subscribe(IObserver<T> observer)
        {
            Contract.Requires<ArgumentNullException>(observer != null);
            Dispatcher.UIThread.VerifyAccess();

            var first = false;

            for (; ; )
            {
                if (Volatile.Read(ref _observers) == null)
                {
                    if (_error != null)
                    {
                        observer.OnError(_error);
                    }
                    else
                    {
                        observer.OnCompleted();
                    }

                    return Disposable.Empty;
                }

                lock (this)
                {
                    if (_observers == null)
                    {
                        continue;
                    }

                    first = _observers.Count == 0;
                    _observers.Add(observer);
                    break;
                }
            }

            if (first)
            {
                Initialize();
            }

            Subscribed(observer, first);

            return new RemoveObserver(this, observer);
        }

        void Remove(IObserver<T> observer)
        {
            if (Volatile.Read(ref _observers) != null)
            {
                lock (this)
                {
                    var observers = _observers;

                    if (observers != null)
                    {
                        observers.Remove(observer);

                        if (observers.Count == 0)
                        {
                            observers.TrimExcess();
                        }
                        else
                        {
                            return;
                        }
                    } else
                    {
                        return;
                    }
                }

                Deinitialize();
            }
        }

        sealed class RemoveObserver : IDisposable
        {
            LightweightObservableBase<T> _parent;

            IObserver<T> _observer;

            public RemoveObserver(LightweightObservableBase<T> parent, IObserver<T> observer)
            {
                _parent = parent;
                Volatile.Write(ref _observer, observer);
            }

            public void Dispose()
            {
                var observer = _observer;
                Interlocked.Exchange(ref _parent, null)?.Remove(observer);
                _observer = null;
            }
        }

        protected abstract void Initialize();
        protected abstract void Deinitialize();

        protected void PublishNext(T value)
        {
            if (Volatile.Read(ref _observers) != null)
            {
                IObserver<T>[] observers;

                lock (this)
                {
                    if (_observers == null)
                    {
                        return;
                    }
                    observers = _observers.ToArray();
                }

                foreach (var observer in observers)
                {
                    observer.OnNext(value);
                }
            }
        }

        protected void PublishCompleted()
        {
            if (Volatile.Read(ref _observers) != null)
            {
                IObserver<T>[] observers;

                lock (this)
                {
                    if (_observers == null)
                    {
                        return;
                    }
                    observers = _observers.ToArray();
                    Volatile.Write(ref _observers, null);
                }

                foreach (var observer in observers)
                {
                    observer.OnCompleted();
                }

                Deinitialize();
            }
        }

        protected void PublishError(Exception error)
        {
            if (Volatile.Read(ref _observers) != null)
            {

                IObserver<T>[] observers;

                lock (this)
                {
                    if (_observers == null)
                    {
                        return;
                    }

                    _error = error;
                    observers = _observers.ToArray();
                    Volatile.Write(ref _observers, null);
                }

                foreach (var observer in observers)
                {
                    observer.OnError(error);
                }

                Deinitialize();
            }
        }

        protected virtual void Subscribed(IObserver<T> observer, bool first)
        {
        }
    }
}
