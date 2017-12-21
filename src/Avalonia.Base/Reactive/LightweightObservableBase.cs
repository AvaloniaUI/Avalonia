using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
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

            if (_observers == null)
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

            var first = _observers.Count == 0;

            lock (_observers)
            {
                _observers.Add(observer);
            }

            if (first)
            {
                Initialize();
            }

            Subscribed(observer, first);

            return Disposable.Create(() =>
            {
                if (_observers != null)
                {
                    lock (_observers)
                    {
                        _observers?.Remove(observer);

                        if (_observers?.Count == 0)
                        {
                            Deinitialize();
                            _observers.TrimExcess();
                        }
                    }
                }
            });
        }

        protected abstract void Initialize();
        protected abstract void Deinitialize();

        protected void PublishNext(T value)
        {
            if (_observers != null)
            {
                IObserver<T>[] observers;

                lock (_observers)
                {
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
            if (_observers != null)
            {
                IObserver<T>[] observers;

                lock (_observers)
                {
                    observers = _observers.ToArray();
                    _observers = null;
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
            if (_observers != null)
            {
                IObserver<T>[] observers;

                lock (_observers)
                {
                    observers = _observers.ToArray();
                    _observers = null;
                }

                foreach (var observer in observers)
                {
                    observer.OnError(error);
                }

                _error = error;
                Deinitialize();
            }
        }

        protected virtual void Subscribed(IObserver<T> observer, bool first)
        {
        }
    }
}
