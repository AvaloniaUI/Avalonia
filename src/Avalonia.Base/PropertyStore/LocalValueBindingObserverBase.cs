using System;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.PropertyStore
{
    internal class LocalValueBindingObserverBase<T> : IObserver<T>,
        IObserver<BindingValue<T>>,
        IDisposable
    {
        private readonly ValueStore _owner;
        private readonly bool _hasDataValidation;
        protected IDisposable? _subscription;
        private T? _defaultValue;
        private bool _isDefaultValueInitialized;

        protected LocalValueBindingObserverBase(ValueStore owner, StyledProperty<T> property)
        {
            _owner = owner;
            Property = property;
            _hasDataValidation = property.GetMetadata(owner.Owner.GetType()).EnableDataValidation ?? false;
        }

        public StyledProperty<T> Property { get;}

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
            OnCompleted();
        }

        public void OnCompleted()
        {
            if (_hasDataValidation)
                _owner.Owner.OnUpdateDataValidation(Property, BindingValueType.UnsetValue, null);

            _owner.OnLocalValueBindingCompleted(Property, this);
        }

        public void OnError(Exception error) => OnCompleted();

        public void OnNext(T value)
        {
            static void Execute(LocalValueBindingObserverBase<T> instance, T value)
            {
                var owner = instance._owner;
                var property = instance.Property;

                if (property.ValidateValue?.Invoke(value) == false)
                    value = instance.GetCachedDefaultValue();

                owner.SetLocalValue(property, value);

                if (instance._hasDataValidation)
                    owner.Owner.OnUpdateDataValidation(property, BindingValueType.Value, null);
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                Execute(this, value);
            }
            else
            {
                // To avoid allocating closure in the outer scope we need to capture variables
                // locally. This allows us to skip most of the allocations when on UI thread.
                var instance = this;
                var newValue = value;
                Dispatcher.UIThread.Post(() => Execute(instance, newValue));
            }
        }

        public void OnNext(BindingValue<T> value)
        {
            static void Execute(LocalValueBindingObserverBase<T> instance, BindingValue<T> value)
            {
                var owner = instance._owner;
                var property = instance.Property;
                var originalType = value.Type;

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

            if (value.Type is BindingValueType.DoNothing)
                return;

            if (Dispatcher.UIThread.CheckAccess())
            {
                Execute(this, value);
            }
            else
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
