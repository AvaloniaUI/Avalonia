using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    internal abstract class BindingEntryBase<TValue, TSource> : IValueEntry,
        IObserver<TSource>,
        IObserver<BindingValue<TSource>>,
        IDisposable
    {
        private static IDisposable s_creating = Disposable.Empty;
        private static IDisposable s_creatingQuiet = Disposable.Create(() => { });
        private readonly ValueFrame _frame;
        private IDisposable? _subscription;
        private bool _hasValue;
        private TValue? _value;

        protected BindingEntryBase(
            ValueFrame frame,
            AvaloniaProperty property,
            IObservable<BindingValue<TSource>> source)
        {
            _frame = frame;
            Source = source;
            Property = property;
        }

        protected BindingEntryBase(
            ValueFrame frame,
            AvaloniaProperty property,
            IObservable<TSource> source)
        {
            _frame = frame;
            Source = source;
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

        public bool IsSubscribed => _subscription is not null;
        public AvaloniaProperty Property { get; }
        AvaloniaProperty IValueEntry.Property => Property;
        protected object Source { get; }

        public void Dispose()
        {
            Unsubscribe();
            BindingCompleted();
        }

        public TValue GetValue()
        {
            Start(produceValue: false);
            if (!_hasValue)
                throw new AvaloniaInternalException("The binding entry has no value.");
            return _value!;
        }

        public void Start() => Start(true);

        public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
        {
            Start(produceValue: false);
            value = _value;
            return _hasValue;
        }

        public void OnCompleted() => BindingCompleted();
        public void OnError(Exception error) => BindingCompleted();
        public void OnNext(TSource value) => SetValue(ConvertAndValidate(value));
        public void OnNext(BindingValue<TSource> value) => SetValue(ConvertAndValidate(value));

        public virtual void Unsubscribe()
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

        protected abstract BindingValue<TValue> ConvertAndValidate(TSource value);
        protected abstract BindingValue<TValue> ConvertAndValidate(BindingValue<TSource> value);

        protected virtual void Start(bool produceValue)
        {
            if (_subscription is not null)
                return;

            _subscription = produceValue ? s_creating : s_creatingQuiet;
            _subscription = Source switch
            {
                IObservable<BindingValue<TSource>> bv => bv.Subscribe(this),
                IObservable<TSource> b => b.Subscribe(this),
                _ => throw new AvaloniaInternalException("Unexpected binding source."),
            };
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

        private void SetValue(BindingValue<TValue> value)
        {
            if (_frame.Owner is null)
                return;

            LoggingUtils.LogIfNecessary(_frame.Owner.Owner, Property, value);

            if (value.HasValue)
            {
                if (!_hasValue || !EqualityComparer<TValue>.Default.Equals(_value, value.Value))
                {
                    _value = value.Value;
                    _hasValue = true;
                    if (_subscription is not null && _subscription != s_creatingQuiet)
                        _frame.Owner?.OnBindingValueChanged(Property, _frame.Priority, value.Value);
                }
            }
            else if (value.Type != BindingValueType.DoNothing)
            {
                ClearValue();
                if (_subscription is not null && _subscription != s_creatingQuiet)
                    _frame.Owner?.OnBindingValueCleared(Property, _frame.Priority);
            }
        }

        private void BindingCompleted()
        {
            _subscription = null;
            _frame.OnBindingCompleted(this);
        }
    }
}
