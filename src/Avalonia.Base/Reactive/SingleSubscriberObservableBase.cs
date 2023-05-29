using System;
using Avalonia.Threading;

namespace Avalonia.Reactive
{
    internal abstract class SingleSubscriberObservableBase<T> : IObservable<T>, IDisposable
    {
        private Exception? _error;
        private IObserver<T>? _observer;
        private bool _completed;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            _ = observer ?? throw new ArgumentNullException(nameof(observer));
            Dispatcher.UIThread.VerifyAccess();

            if (_observer != null)
            {
                throw new InvalidOperationException("The observable can only be subscribed once.");
            }

            if (_error != null)
            {
                observer.OnError(_error);
            }
            else if (_completed)
            {
                observer.OnCompleted();
            }
            else
            {
                _observer = observer;
                Subscribed();
            }

            return this;
        }

        public virtual void Dispose()
        {
            Unsubscribed();
            _observer = null;
        }

        protected abstract void Unsubscribed();

        protected void PublishNext(T value)
        {
            _observer?.OnNext(value);
        }

        protected void PublishCompleted()
        {
            _completed = true;

            if (_observer != null)
            {
                _observer.OnCompleted();
                Unsubscribed();
                _observer = null;
            }
        }

        protected void PublishError(Exception error)
        {
            _error = error;

            if (_observer != null)
            {
                _observer.OnError(error);
                Unsubscribed();
                _observer = null;
            }
        }

        protected abstract void Subscribed();
    }
}
