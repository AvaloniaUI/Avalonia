using System;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.PropertyStore
{
    internal class LocalValueUntypedBindingObserver<T> : IObserver<object?>,
        IDisposable
    {
        private readonly ValueStore _owner;
        private IDisposable? _subscription;
        private T? _defaultValue;
        private bool _isDefaultValueInitialized;

        public LocalValueUntypedBindingObserver(ValueStore owner, StyledProperty<T> property)
        {
            _owner = owner;
            Property = property;
        }

        public StyledProperty<T> Property { get; }

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
            static void Execute(LocalValueUntypedBindingObserver<T> instance, object? value)
            {
                var owner = instance._owner;
                var property = instance.Property;

                var valueType = BindingValueType.Value;
                Exception? error = null;

                if (value is BindingNotification n)
                {
                    value = n.Value;
                    LoggingUtils.LogIfNecessary(owner.Owner, property, n);

                    valueType = n.ErrorType switch
                    {
                        BindingErrorType.None => n.HasValue ? BindingValueType.Value : BindingValueType.UnsetValue,
                        BindingErrorType.DataValidationError => BindingValueType.DataValidationError,
                        BindingErrorType.Error => BindingValueType.BindingError,
                        _ => throw new NotImplementedException(),
                    };
                    error = n.Error;
                }

                if (value == AvaloniaProperty.UnsetValue)
                {
                    owner.SetValue(property, instance.GetCachedDefaultValue(), BindingPriority.LocalValue);
                    if (valueType == BindingValueType.Value) // happens when the method was called with AvaloniaProperty.UnsetValue itself
                        valueType = BindingValueType.UnsetValue;
                }
                else if (UntypedValueUtils.TryConvertAndValidate(property, value, out var typedValue))
                {
                    owner.SetValue(property, typedValue, BindingPriority.LocalValue);
                }
                else
                {
                    owner.SetValue(property, instance.GetCachedDefaultValue(), BindingPriority.LocalValue);
                    LoggingUtils.LogInvalidValue(owner.Owner, property, typeof(T), value);
                }

                owner.Owner.UpdateDataValidationCore(property, valueType, error);
            }

            if (value == BindingOperations.DoNothing)
                return;

            if (Dispatcher.UIThread.CheckAccess())
            {
                Execute(this, value);
            }
            else if (value != BindingOperations.DoNothing)
            {
                // To avoid allocating closure in the outer scope we need to capture variables
                // locally. This allows us to skip most of the allocations when on UI thread.
                var instance = this;
                var newValue = value;
                Dispatcher.UIThread.Post(() => Execute(instance, newValue));
            }
        }

        private T GetCachedDefaultValue()
        {
            if (!_isDefaultValueInitialized)
            {
                _defaultValue = Property.GetDefaultValue(_owner.Owner.GetType());
                _isDefaultValueInitialized = true;
            }

            return _defaultValue!;
        }
    }
}
