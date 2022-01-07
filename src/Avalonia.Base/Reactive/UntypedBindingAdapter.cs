using System;
using System.Reactive.Subjects;
using Avalonia.Data;

namespace Avalonia.Reactive
{
    internal class UntypedBindingAdapter<T> : SingleSubscriberObservableBase<object?>,
        IObserver<BindingValue<T>>
    {
        private readonly IObservable<BindingValue<T>> _source;
        private IDisposable? _subscription;

        public UntypedBindingAdapter(IObservable<BindingValue<T>> source) => _source = source;
        public void OnCompleted() => PublishCompleted();
        public void OnError(Exception error) => PublishError(error);
        public void OnNext(BindingValue<T> value) => value.ToUntyped();
        protected override void Subscribed() => _subscription = _source.Subscribe(this);
        protected override void Unsubscribed() => _subscription?.Dispose();
    }

    internal class UntypedBindingSubjectAdapter<T> : SingleSubscriberObservableBase<object?>,
        ISubject<object?>
    {
        private readonly ISubject<BindingValue<T>> _source;
        private readonly Inner _inner;
        private IDisposable? _subscription;

        public UntypedBindingSubjectAdapter(ISubject<BindingValue<T>> source)
        {
            _source = source;
            _inner = new Inner(this);
        }

        public void OnCompleted() => _source.OnCompleted();
        public void OnError(Exception error) => _source.OnError(error);
        public void OnNext(object? value)
        {
            _source.OnNext(BindingValue<T>.FromUntyped(value));
        }

        protected override void Subscribed() => _subscription = _source.Subscribe(_inner);
        protected override void Unsubscribed() => _subscription?.Dispose();

        private class Inner : IObserver<BindingValue<T>>
        {
            private readonly UntypedBindingSubjectAdapter<T> _owner;

            public Inner(UntypedBindingSubjectAdapter<T> owner) => _owner = owner;

            public void OnCompleted() => _owner.PublishCompleted();
            public void OnError(Exception error) => _owner.PublishError(error);
            public void OnNext(BindingValue<T> value) => _owner.PublishNext(value.ToUntyped());
        }
    }
}
