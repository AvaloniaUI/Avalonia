using System;
using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Animation
{
    public abstract class TransitionObservableBase<T> : SingleSubscriberObservableBase<T>, IObserver<double>
    {
        private readonly IObservable<double> _progress;
        private IDisposable? _progressSubscription;

        protected TransitionObservableBase(IObservable<double> progress)
        {
            _progress = progress;
        }

        protected override void Unsubscribed()
        {
            _progressSubscription?.Dispose();
        }

        protected override void Subscribed()
        {
            _progressSubscription = _progress.Subscribe(this);
        }

        protected abstract T ProduceValue(double progress);

        void IObserver<double>.OnCompleted()
        {
            PublishCompleted();
        }

        void IObserver<double>.OnError(Exception error)
        {
            PublishError(error);
        }

        void IObserver<double>.OnNext(double value)
        {
            PublishNext(ProduceValue(value));
        }
    }
}
