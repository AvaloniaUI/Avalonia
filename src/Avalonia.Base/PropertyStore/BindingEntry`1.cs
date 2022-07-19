using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    internal class BindingEntry<T> : IValueEntry<T>,
        IObserver<T>,
        IObserver<BindingValue<T>>,
        IDisposable
    {
        private readonly ValueFrameBase _frame;
        private readonly object _source;
        private IDisposable? _subscription;
        private bool _hasValue;
        private T? _value;

        public BindingEntry(
            ValueFrameBase frame,
            StyledPropertyBase<T> property,
            IObservable<BindingValue<T>> source)
        {
            _frame = frame;
            _source = source;
            Property = property;
        }

        public BindingEntry(
            ValueFrameBase frame,
            StyledPropertyBase<T> property,
            IObservable<T> source)
        {
            _frame = frame;
            _source = source;
            Property = property;
        }

        public bool HasValue
        {
            get
            {
                StartIfNecessary();
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
            StartIfNecessary();
            if (!_hasValue)
                throw new AvaloniaInternalException("The binding entry has no value.");
            return _value!;
        }

        public void Start()
        {
            Debug.Assert(_subscription is null);

            // Subscription won't be set until Subscribe completes, but in the meantime we
            // need to signal that we've started as Subscribe may cause a value to be produced.
            _subscription = Disposable.Empty;

            if (_source is IObservable<BindingValue<T>> bv)
                _subscription = bv.Subscribe(this);
            else if (_source is IObservable<T> b)
                _subscription = b.Subscribe(this);
            else
                throw new AvaloniaInternalException("Unexpected binding source.");
        }

        public bool TryGetValue([MaybeNullWhen(false)] out T value)
        {
            StartIfNecessary();
            value = _value;
            return _hasValue;
        }

        public void OnCompleted() => BindingCompleted();
        public void OnError(Exception error) => BindingCompleted();

        public void OnNext(T value) => SetValue(value);

        public void OnNext(BindingValue<T> value)
        {
            if (_frame.Owner is not null)
                LoggingUtils.LogIfNecessary(_frame.Owner.Owner, Property, value);

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
            StartIfNecessary();
            if (!_hasValue)
                throw new AvaloniaInternalException("The BindingEntry<T> has no value.");
            return _value!;
        }

        bool IValueEntry.TryGetValue(out object? value)
        {
            StartIfNecessary();
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

        private void SetValue(T value)
        {
            if (_frame.Owner is null)
                return;

            if (Property.ValidateValue?.Invoke(value) != false)
            {
                if (!_hasValue || !EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    _hasValue = true;
                    _frame.Owner?.OnBindingValueChanged(Property, _frame.Priority, value);
                }
            }
            else
            {
                _frame.Owner?.OnBindingValueCleared(Property, _frame.Priority);
            }
        }

        private void BindingCompleted()
        {
            _subscription = null;
            _frame.OnBindingCompleted(this);
        }

        private void StartIfNecessary()
        {
            if (_subscription is null)
                Start();
        }
    }
}
