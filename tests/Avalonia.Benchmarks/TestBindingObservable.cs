#nullable enable
using System;
using Avalonia.Data;

namespace Avalonia.Benchmarks
{
    internal class TestBindingObservable<T> : IObservable<BindingValue<T?>>, IDisposable
    {
        private T? _value;
        private IObserver<BindingValue<T?>>? _observer;

        public TestBindingObservable(T? initialValue = default) => _value = initialValue;

        public IDisposable Subscribe(IObserver<BindingValue<T?>> observer)
        {
            if (_observer is object)
                throw new InvalidOperationException("The observable can only be subscribed once.");

            _observer = observer;
            observer.OnNext(_value);
            return this;
        }

        public void Dispose() => _observer = null;
        public void OnNext(T? value) => _observer?.OnNext(value);

        public void PublishCompleted()
        {
            _observer?.OnCompleted();
            _observer = null;
        }

        protected void PublishError(Exception error)
        {
            _observer?.OnError(error);
            _observer = null;
        }
    }
}
