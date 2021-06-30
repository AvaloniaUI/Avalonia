using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Data;
using Avalonia.Logging;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal interface ILocalValueEntry : IValueEntry
    {
        IDisposable AddBinding(IObservable<object?> source);
        void ClearValue();
    }

    internal class LocalValueEntry<T> : IValueEntry<T>,
        ILocalValueEntry,
        IObserver<BindingValue<T>>,
        IObserver<object?>,
        IDisposable
    {
        private readonly LocalValueFrame _owner;
        private IDisposable? _bindingSubscription;
        private TypedObserverShim? _shim;
        private bool _hasValue;
        private T? _value;

        public LocalValueEntry(LocalValueFrame owner, StyledPropertyBase<T> property)
        {
            _owner = owner;
            Property = property;
        }

        public bool HasValue => _hasValue;
        public StyledPropertyBase<T> Property { get; }
        AvaloniaProperty IValueEntry.Property => Property;

        public IDisposable AddBinding(IObservable<BindingValue<T>> source)
        {
            _bindingSubscription?.Dispose();
            _bindingSubscription = Disposable.Empty;
            _bindingSubscription = source.Subscribe(this);
            return this;
        }

        public IDisposable AddBinding(IObservable<T?> source)
        {
            _shim ??= new TypedObserverShim(this);
            _bindingSubscription?.Dispose();
            _bindingSubscription = Disposable.Empty;
            _bindingSubscription = source.Subscribe(_shim);
            return this;
        }

        public IDisposable AddBinding(IObservable<object?> source)
        {
            _bindingSubscription?.Dispose();
            _bindingSubscription = Disposable.Empty;
            _bindingSubscription = source.Subscribe(this);
            return this;
        }

        public void ClearValue()
        {
            if (_bindingSubscription is null)
                _owner.Remove(this);

            if (_hasValue)
            {
                var oldValue = _hasValue ? new Optional<T>(_value) : default;
                _hasValue = false;
                _value = default;
                _owner.ValueStore.ValueChanged(_owner, this, oldValue);
            }
        }

        public void Dispose()
        {
            _bindingSubscription?.Dispose();
            BindingCompleted();
        }

        public void SetValue(T? value)
        {
            if (Property.ValidateValue?.Invoke(value) == false)
            {
                value = Property.GetDefaultValue(_owner.ValueStore.Owner.GetType());
            }

#pragma warning disable CS8604 // Possible null reference argument.
            if (!_hasValue || !EqualityComparer<T>.Default.Equals(_value, value))
#pragma warning restore CS8604 // Possible null reference argument.
            {
                var oldValue = _hasValue ? new Optional<T>(_value) : default;
                _value = value;
                _hasValue = true;
                _owner.ValueStore.ValueChanged<T>(_owner, this, oldValue);
            }
        }

        public void SetValue(object? value)
        {
            if (value == BindingOperations.DoNothing)
                return;

            if (value is BindingNotification notification)
            {
                if (notification.Error is object)
                    _owner.ValueStore.Owner.LogBindingError(Property, notification.Error);
                value = BindingNotification.ExtractValue(value);
            }

            if (value == AvaloniaProperty.UnsetValue)
                ClearValue();
            else if (value is T v)
                SetValue(v);
            else if (value is null && !typeof(T).IsValueType)
                SetValue(default);
            else
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Binding)?.Log(
                    _owner.ValueStore.Owner,
                    "Binding produced invalid value: expected {ExpectedType}, got {Value} ({ValueType})",
                    typeof(T),
                    value,
                    value?.GetType());
                SetValue(AvaloniaProperty.UnsetValue);
            }
        }

        public bool TryGetValue(out T? value)
        {
            value = _value;
            return _hasValue;
        }

        public bool TryGetValue(out object? value)
        {
            value = _value;
            return _hasValue;
        }

        public void OnCompleted() => BindingCompleted();
        public void OnError(Exception error) => BindingCompleted();
        void IObserver<object?>.OnNext(object? value) => SetValue(value);

        void IObserver<BindingValue<T>>.OnNext(BindingValue<T> value)
        {
            if (value.Error is object)
                _owner.ValueStore.Owner.LogBindingError(Property, value.Error);
            if (value.HasValue)
                SetValue(value.Value);
            else if (value.Type == BindingValueType.BindingError)
                ClearValue();
        }

        private void BindingCompleted()
        {
            _bindingSubscription = null;
            ClearValue();
        }

        private class TypedObserverShim : IObserver<T?>
        {
            private readonly LocalValueEntry<T> _owner;
            public TypedObserverShim(LocalValueEntry<T> owner) => _owner = owner;
            public void OnCompleted() => _owner.BindingCompleted();
            public void OnError(Exception error) => _owner.BindingCompleted();
            public void OnNext(T? value) => _owner.SetValue(value);
        }
    }
}
