using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.PropertyStore
{
    internal class LocalValueUntypedBindingObserver<T> : IObserver<object?>,
        IDisposable
    {
        private readonly ValueStore _owner;
        private readonly bool _hasDataValidation;
        private IDisposable? _subscription;
        private T? _defaultValue;
        private bool _isDefaultValueInitialized;

        public LocalValueUntypedBindingObserver(ValueStore owner, StyledProperty<T> property)
        {
            _owner = owner;
            Property = property;
            _hasDataValidation = property.GetMetadata(owner.Owner.GetType()).EnableDataValidation ?? false;
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

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConvertionSupressWarningMessage)]
        public void OnNext(object? value)
        {
            static void Execute(LocalValueUntypedBindingObserver<T> instance, object? untypedValue)
            {
                var owner = instance._owner;
                var property = instance.Property;
                var value = BindingValue<T>.FromUntyped(untypedValue, property.PropertyType);
                var originalType = value.Type;

                LoggingUtils.LogIfNecessary(owner.Owner, property, value);

                // Revert to the default value if the binding value fails validation, or if
                // there was no value (though not if there was a data validation error).
                if ((value.HasValue && property.ValidateValue?.Invoke(value.Value) == false) ||
                    (!value.HasValue && value.Type != BindingValueType.DataValidationError))
                    value = value.WithValue(instance.GetCachedDefaultValue());

                if (value.HasValue)
                    owner.SetLocalValue(property, value.Value);
                if (instance._hasDataValidation)
                    owner.Owner.OnUpdateDataValidation(property, originalType, value.Error);
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
