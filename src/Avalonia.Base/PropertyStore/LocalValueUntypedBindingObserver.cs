using System;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    internal class LocalValueUntypedBindingObserver<T> : IObserver<object?>,
        IDisposable
    {
        private readonly ValueStore _owner;
        private IDisposable? _subscription;

        public LocalValueUntypedBindingObserver(ValueStore owner, StyledPropertyBase<T> property)
        {
            _owner = owner;
            Property = property;
        }

        public StyledPropertyBase<T> Property { get; }

        public void Start(IObservable<object?> source)
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

        public void OnNext(object? value)
        {
            if (value is BindingNotification n)
            {
                value = n.Value;
                LoggingUtils.LogIfNecessary(_owner.Owner, Property, n);
            }

            if (value == AvaloniaProperty.UnsetValue)
            {
                _owner.ClearLocalValue(Property);
            }
            else if (value == BindingOperations.DoNothing)
            {
                // Do nothing!
            }
            else if (UntypedValueUtils.TryConvertAndValidate(Property, value, out var typedValue))
            {
                _owner.SetValue(Property, typedValue, BindingPriority.LocalValue);
            }
            else
            {
                _owner.ClearLocalValue(Property);
                LoggingUtils.LogInvalidValue(_owner.Owner, Property, typeof(T), value);
            }
        }
    }
}
