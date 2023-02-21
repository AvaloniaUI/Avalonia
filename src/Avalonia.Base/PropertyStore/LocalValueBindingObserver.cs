using System;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.PropertyStore
{
    internal class LocalValueBindingObserver<T> : IObserver<T>,
        IObserver<BindingValue<T>>,
        IDisposable
    {
        private readonly ValueStore _owner;
        private IDisposable? _subscription;
        private T? _defaultValue;
        private bool _isDefaultValueInitialized;

        public LocalValueBindingObserver(ValueStore owner, StyledProperty<T> property)
        {
            _owner = owner;
            Property = property;
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
            _owner.OnLocalValueBindingCompleted(Property, this);
        }

        public void OnCompleted() => _owner.OnLocalValueBindingCompleted(Property, this);
        public void OnError(Exception error) => OnCompleted();

        public void OnNext(T value)
        {
            static void Execute(LocalValueBindingObserver<T> instance, T value)
            {
                var owner = instance._owner;
                var property = instance.Property;

                if (property.ValidateValue?.Invoke(value) == false)
                    value = instance.GetCachedDefaultValue();

                owner.SetValue(property, value, BindingPriority.LocalValue);
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
            static void Execute(LocalValueBindingObserver<T> instance, BindingValue<T> value)
            {
                var owner = instance._owner;
                var property = instance.Property;

                LoggingUtils.LogIfNecessary(owner.Owner, property, value);

                if (value.HasValue)
                {
                    var effectiveValue = value.Value;
                    if (property.ValidateValue?.Invoke(effectiveValue) == false)
                        effectiveValue = instance.GetCachedDefaultValue();
                    owner.SetValue(property, effectiveValue, BindingPriority.LocalValue);
                }
                else
                {
                    owner.SetValue(property, instance.GetCachedDefaultValue(), BindingPriority.LocalValue);
                }
            }

            if (value.Type is BindingValueType.DoNothing or BindingValueType.DataValidationError)
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
