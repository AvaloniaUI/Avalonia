using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    internal class UntypedBindingEntry<T> : IValueEntry<T>,
        IObserver<object?>,
        IDisposable
    {
        private readonly ValueFrameBase _frame;
        private readonly IObservable<object?> _source;
        private IDisposable? _subscription;
        private bool _hasValue;
        private T? _value;

        public UntypedBindingEntry(
            ValueFrameBase frame,
            StyledPropertyBase<T> property,
            IObservable<object?> source)
        {
            _frame = frame;
            _source = source;
            Property = property;
        }

        public bool HasValue
        {
            get
            {
                Start(produceValue: false);
                return _hasValue;
            }
        }

        public StyledPropertyBase<T> Property { get; }
        AvaloniaProperty IValueEntry.Property => Property;

        public void Dispose()
        {
            Unsubscribe();
            BindingCompleted();
        }

        public T GetValue()
        {
            Start(produceValue: false);
            if (!_hasValue)
                throw new AvaloniaInternalException("The binding entry has no value.");
            return _value!;
        }

        public void Start() => Start(true);

        public bool TryGetValue([MaybeNullWhen(false)] out T value)
        {
            Start(produceValue: false);
            value = _value;
            return _hasValue;
        }

        public void OnCompleted() => BindingCompleted();
        public void OnError(Exception error) => BindingCompleted();

        public void OnNext(object? value) => SetValue(value);

        public void OnNext(BindingValue<T> value)
        {
            if (value.HasValue)
                SetValue(value.Value);
            else
                ClearValue();
        }

        public void Unsubscribe()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        object? IValueEntry.GetValue()
        {
            Start(produceValue: false);
            if (!_hasValue)
                throw new AvaloniaInternalException("The BindingEntry<T> has no value.");
            return _value!;
        }

        bool IValueEntry.TryGetValue(out object? value)
        {
            Start(produceValue: false);
            value = _value;
            return _hasValue;
        }

        private void ClearValue()
        {
            if (_hasValue)
            {
                _hasValue = false;
                _value = default;
                _frame.Owner?.OnBindingValueCleared(Property, _frame.Priority);
            }
        }

        private void SetValue(object? value)
        {
            if (_frame.Owner is null)
                return;

            if (value is BindingNotification n)
            {
                value = n.Value;
                LoggingUtils.LogIfNecessary(_frame.Owner.Owner, Property, n);
            }

            if (value == AvaloniaProperty.UnsetValue)
            {
                ClearValue();
            }
            else if (value == BindingOperations.DoNothing)
            {
                // Do nothing!
            }
            else if (UntypedValueUtils.TryConvertAndValidate(Property, value, out var typedValue))
            {
                if (!_hasValue || !EqualityComparer<T>.Default.Equals(_value, typedValue))
                {
                    _value = typedValue;
                    _hasValue = true;
                    _frame.Owner?.OnBindingValueChanged(Property, _frame.Priority, typedValue);
                }
            }
            else
            {
                ClearValue();
                LoggingUtils.LogInvalidValue(_frame.Owner.Owner, Property, typeof(T), value);
            }
        }

        private void BindingCompleted()
        {
            _subscription = null;
            _frame.OnBindingCompleted(this);
        }

        private void Start(bool produceValue)
        {
            if (_subscription is not null)
                return;

            // Will only produce a new value when subscription isn't null.
            if (produceValue)
                _subscription = Disposable.Empty;

            _subscription = _source.Subscribe(this);
        }
    }
}
