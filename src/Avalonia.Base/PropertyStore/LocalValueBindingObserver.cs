using System;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    internal class LocalValueBindingObserver<T> : IObserver<T>,
        IObserver<BindingValue<T>>,
        IDisposable
    {
        private readonly ValueStore _owner;
        private IDisposable? _subscription;

        public LocalValueBindingObserver(ValueStore owner, StyledPropertyBase<T> property)
        {
            _owner = owner;
            Property = property;
        }

        public StyledPropertyBase<T> Property { get;}

        public void Start(IObservable<T> source)
        {
            _subscription = source.Subscribe(this);
        }

        public void Start(IObservable<BindingValue<T>> source)
        {
            _subscription = source.Subscribe(this);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            _owner.OnLocalValueBindingCompleted(Property, this);
        }

        public void OnCompleted() => _owner.OnLocalValueBindingCompleted(Property, this);
        public void OnError(Exception error) => OnCompleted();

        public void OnNext(T value)
        {
            if (Property.ValidateValue?.Invoke(value) != false)
                _owner.SetValue(Property, value, BindingPriority.LocalValue);
            else
                _owner.ClearLocalValue(Property);
        }

        public void OnNext(BindingValue<T> value)
        {
            LoggingUtils.LogIfNecessary(_owner.Owner, Property, value);

            if (value.HasValue)
                _owner.SetValue(Property, value.Value, BindingPriority.LocalValue);
            else if (value.Type != BindingValueType.DataValidationError)
                _owner.ClearLocalValue(Property);
        }
    }
}
