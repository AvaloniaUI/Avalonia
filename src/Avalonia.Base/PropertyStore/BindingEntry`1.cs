using System;
using System.Collections.Generic;
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
        private static IDisposable s_Creating = Disposable.Empty;
        private static IDisposable s_CreatingQuiet = Disposable.Create(() => { });
        private readonly ValueFrame _frame;
        private readonly object _source;
        private IDisposable? _subscription;
        private bool _hasValue;
        private T? _value;

        public BindingEntry(
            ValueFrame frame,
            StyledPropertyBase<T> property,
            IObservable<BindingValue<T>> source)
        {
            _frame = frame;
            _source = source;
            Property = property;
        }

        public BindingEntry(
            ValueFrame frame,
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
                if (_subscription is not null)
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
                    if (_subscription is not null && _subscription != s_CreatingQuiet)
                        _frame.Owner?.OnBindingValueChanged(Property, _frame.Priority, value);
                }
            }
            else if (_subscription is not null && _subscription != s_CreatingQuiet)
            {
                _frame.Owner?.OnBindingValueCleared(Property, _frame.Priority);
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

            _subscription = produceValue ? s_Creating : s_CreatingQuiet;
            _subscription = _source switch
            {
                IObservable<BindingValue<T>> bv => bv.Subscribe(this),
                IObservable<T> b => b.Subscribe(this),
                _ => throw new AvaloniaInternalException("Unexpected binding source."),
            };
        }
    }
}
