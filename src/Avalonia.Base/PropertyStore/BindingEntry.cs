using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    internal class BindingEntry : IValueEntry,
        IObserver<object?>,
        IDisposable
    {
        private readonly ValueFrameBase _frame;
        private readonly IObservable<object?> _source;
        private IDisposable? _subscription;
        private bool _hasValue;
        private object? _value;

        public BindingEntry(
            ValueFrameBase frame,
            AvaloniaProperty property,
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
                StartIfNecessary();
                return _hasValue;
            }
        }

        public AvaloniaProperty Property { get; }

        public void Dispose()
        {
            Unsubscribe();
            BindingCompleted();
        }

        public object? GetValue()
        {
            StartIfNecessary();
            if (!_hasValue)
                throw new AvaloniaInternalException("The binding entry has no value.");
            return _value!;
        }

        public bool TryGetValue(out object? value)
        {
            StartIfNecessary();
            value = _value;
            return _hasValue;
        }

        public void Start()
        {
            Debug.Assert(_subscription is null);

            // Subscription won't be set until Subscribe completes, but in the meantime we
            // need to signal that we've started as Subscribe may cause a value to be produced.
            _subscription = Disposable.Empty;
            _subscription = _source.Subscribe(this);
        }

        public void OnCompleted() => BindingCompleted();
        public void OnError(Exception error) => BindingCompleted();

        public void OnNext(object? value) => SetValue(value);

        public virtual void Unsubscribe()
        {
            _subscription?.Dispose();
            _subscription = null;
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
                if (!_hasValue || !Equals(_value, typedValue))
                {
                    _value = typedValue;
                    _hasValue = true;
                    _frame.Owner?.OnBindingValueChanged(Property, _frame.Priority, typedValue);
                }
            }
            else
            {
                ClearValue();
                LoggingUtils.LogInvalidValue(_frame.Owner.Owner, Property, Property.PropertyType, value);
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
