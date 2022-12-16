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
            static void Execute(ValueStore owner, StyledPropertyBase<T> property, T value)
            {
                if (property.ValidateValue?.Invoke(value) != false)
                    owner.SetValue(property, value, BindingPriority.LocalValue);
                else
                    owner.ClearLocalValue(property);
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                Execute(_owner, Property, value);
            }
            else
            {
                // To avoid allocating closure in the outer scope we need to capture variables
                // locally. This allows us to skip most of the allocations when on UI thread.
                var instance = _owner;
                var property = Property;
                var newValue = value;
                Dispatcher.UIThread.Post(() => Execute(instance, property, newValue));
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
                    owner.SetValue(property, value.Value, BindingPriority.LocalValue);
                else if (value.Type != BindingValueType.DataValidationError)
                    owner.ClearLocalValue(property);
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
    }
}
