using System;
using System.Reactive.Subjects;
using Avalonia.Data;

namespace Avalonia.Reactive
{
    internal class BindingValueAdapter<T> : SingleSubscriberObservableBase<BindingValue<T>>,
        IObserver<T>
    {
        private readonly IObservable<T> _source;
        private IDisposable? _subscription;

        public BindingValueAdapter(IObservable<T> source) => _source = source;
        public void OnCompleted() => PublishCompleted();
        public void OnError(Exception error) => PublishError(error);
        public void OnNext(T value) => PublishNext(BindingValue<T>.FromUntyped(value));
        protected override void Subscribed() => _subscription = _source.Subscribe(this);
        protected override void Unsubscribed() => _subscription?.Dispose();
    }

    internal class BindingValueSubjectAdapter<T> : SingleSubscriberObservableBase<BindingValue<T>>,
        ISubject<BindingValue<T>>
    {
        private readonly ISubject<T> _source;
        private readonly Inner _inner;
        private IDisposable? _subscription;

        public BindingValueSubjectAdapter(ISubject<T> source)
        {
            _source = source;
            _inner = new Inner(this);
        }

        public void OnCompleted() => _source.OnCompleted();
        public void OnError(Exception error) => _source.OnError(error);
        
        public void OnNext(BindingValue<T> value)
        {
            if (value.HasValue)
            {
                _source.OnNext(value.Value);
            }
        }

        protected override void Subscribed() => _subscription = _source.Subscribe(_inner);
        protected override void Unsubscribed() => _subscription?.Dispose();

        private class Inner : IObserver<T>
        {
            private readonly BindingValueSubjectAdapter<T> _owner;

            public Inner(BindingValueSubjectAdapter<T> owner) => _owner = owner;

            public void OnCompleted() => _owner.PublishCompleted();
            public void OnError(Exception error) => _owner.PublishError(error);
            public void OnNext(T value) => _owner.PublishNext(BindingValue<T>.FromUntyped(value));
        }
    }
}
