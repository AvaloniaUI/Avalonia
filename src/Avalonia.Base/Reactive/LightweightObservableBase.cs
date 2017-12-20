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

            lock (_observers)
            {
                _observers.Add(observer);
            }

            if (_observers.Count == 1)
            {
                Initialize();
            }

            Subscribed(observer);

            return Disposable.Create(() =>
            {
                _observers?.Remove(observer);

                if (_observers?.Count == 0)
                {
                    Deinitialize();
                    _observers.TrimExcess();
                }
            });
        }

        protected abstract void Initialize();
        protected abstract void Deinitialize();

        protected void PublishNext(T value)
        {
            lock (_observers)
            {
                foreach (var observer in _observers)
                {
                    observer.OnNext(value);
                }
            }
        }

        protected void PublishCompleted()
        {
            lock (_observers)
            {
                foreach (var observer in _observers)
                {
                    observer.OnCompleted();
                }

                _observers = null;
            }

            Deinitialize();
        }

        protected void PublishError(Exception error)
        {
            lock (_observers)
            {
                foreach (var observer in _observers)
                {
                    observer.OnError(error);
                }

                _observers = null;
            }

            _error = error;
            Deinitialize();
        }

        protected virtual void Subscribed(IObserver<T> observer)
        {
        }
    }
}
