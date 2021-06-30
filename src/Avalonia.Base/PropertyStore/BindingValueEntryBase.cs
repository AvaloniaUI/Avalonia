using System;
using System.Reactive.Disposables;
using Avalonia.Data;
using Avalonia.Logging;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal abstract class BindingValueEntryBase : IValueEntry, IObserver<object?>, IDisposable
    {
        private readonly IObservable<object?> _source;
        private IDisposable? _bindingSubscription;
        private object? _value = AvaloniaProperty.UnsetValue;

        public BindingValueEntryBase(
            AvaloniaProperty property,
            IObservable<object?> source)
        {
            _source = source;
            Property = property;
        }

        public bool HasValue
        {
            get
            {
                StartIfNecessary();
                return _value != AvaloniaProperty.UnsetValue;
            }
        }
        
        public bool IsActive => true;
        public AvaloniaProperty Property { get; }
        
        public virtual void Dispose()
        {
            _bindingSubscription?.Dispose();
            BindingCompleted();
        }

        public bool TryGetValue(out object? value)
        {
            StartIfNecessary();
            value = _value;
            return HasValue;
        }

        public void OnCompleted() => BindingCompleted();
        public void OnError(Exception error) => BindingCompleted();
        void IObserver<object?>.OnNext(object? value) => SetValue(value);

        protected abstract AvaloniaObject GetOwner();
        protected abstract void ValueChanged(object? oldValue);
        protected abstract void Completed(object? oldValue);

        private void SetValue(object? value)
        {
            if (value == BindingOperations.DoNothing)
                return;

            if (value != AvaloniaProperty.UnsetValue)
            {
                var accessor = (IStyledPropertyAccessor)Property;

                if (!accessor.ValidateValue(value))
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Binding)?.Log(
                        GetOwner(),
                        "Binding produced invalid value: expected {ExpectedType}, got {Value} ({ValueType})",
                        Property.PropertyType,
                        value,
                        value?.GetType());
                    value = AvaloniaProperty.UnsetValue;
                }

                value = BindingNotification.ExtractValue(value);
            }

            if (!Equals(_value, value))
            {
                var oldValue = _value;
                _value = value;

                // Only raise a property changed notifcation if we're not currently in the process of
                // starting the binding (in this case the value will be read immediately afterwards
                // and a notification raised).
                if (_bindingSubscription != Disposable.Empty)
                    ValueChanged(oldValue);
            }
        }

        private void StartIfNecessary()
        {
            if (_bindingSubscription is null)
            {
                // Prevent reentrancy by first assigning the subscription to a dummy
                // non-null value.
                _bindingSubscription = Disposable.Empty;
                _bindingSubscription = _source.Subscribe(this);
            }
        }

        private void BindingCompleted()
        {
            _bindingSubscription = null;
            _value = AvaloniaProperty.UnsetValue;
            Completed(_value);
        }
    }
}
