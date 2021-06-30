using System;
using Avalonia.Data;

namespace Avalonia.Base.UnitTests
{
    internal class TestStyleBinding : IObservable<object?>, IBinding, IDisposable
    {
        private object? _value;
        private IObserver<object?>? _observer;

        public TestStyleBinding(object? initialValue = null) => _value = initialValue;

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            if (_observer is object)
                throw new InvalidOperationException("The observable can only be subscribed once.");

            _observer = observer;
            observer.OnNext(_value);
            return this;
        }

        public void Dispose() => _observer = null;
        public void OnNext(object? value) => _observer?.OnNext(value);

        public void PublishCompleted()
        {
            _observer?.OnCompleted();
            _observer = null;
        }

        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false)
        {
            return InstancedBinding.OneWay(this, BindingPriority.Style);
        }

        protected void PublishError(Exception error)
        {
            _observer?.OnError(error);
            _observer = null;
        }
    }
}
