using System;
using System.Collections.Generic;
using Avalonia.Reactive;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.PropertyStore
{
    internal abstract class BindingEntryBase<TValue, TSource> : IValueEntry<TValue>,
        IObserver<TSource>,
        IObserver<BindingValue<TSource>>,
        IDisposable
    {
        private static readonly IDisposable s_creating = Disposable.Empty;
        private static readonly IDisposable s_creatingQuiet = Disposable.Create(() => { });
        private IDisposable? _subscription;
        private bool _hasValue;
        private TValue? _value;
        private UncommonFields? _uncommon;

        protected BindingEntryBase(
            AvaloniaObject target,
            ValueFrame frame,
            AvaloniaProperty property,
            IObservable<BindingValue<TSource>> source)
            : this(target, frame, property, (object)source)
        {
        }

        protected BindingEntryBase(
            AvaloniaObject target,
            ValueFrame frame,
            AvaloniaProperty property,
            IObservable<TSource> source)
            : this(target, frame, property, (object)source)
        {
        }

        private BindingEntryBase(
            AvaloniaObject target,
            ValueFrame frame,
            AvaloniaProperty property,
            object source)
        {
            Frame = frame;
            Property = property;
            Source = source;
            if (property.GetMetadata(target.GetType()).EnableDataValidation == true)
                _uncommon = new() { _hasDataValidation = true };
        }

        public bool IsSubscribed => _subscription is not null;
        public AvaloniaProperty Property { get; }
        AvaloniaProperty IValueEntry.Property => Property;
        protected ValueFrame Frame { get; }
        protected object Source { get; }

        public void Dispose()
        {
            Unsubscribe();
            BindingCompleted();
        }

        public bool HasValue()
        {
            Start(produceValue: false);
            return _hasValue;
        }

        public TValue GetValue()
        {
            Start(produceValue: false);
            if (!_hasValue)
                throw new AvaloniaInternalException("The binding entry has no value.");
            return _value!;
        }

        public bool GetDataValidationState(out BindingValueType state, out Exception? error)
        {
            if (_uncommon?._hasDataValidation == true)
            {
                state = _uncommon._dataValidationState;
                error = _uncommon._dataValidationError;
                return true;
            }

            state = BindingValueType.Value;
            error = null;
            return false;
        }

        public void Start() => Start(true);

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

        protected abstract BindingValue<TValue> ConvertAndValidate(TSource value);
        protected abstract BindingValue<TValue> ConvertAndValidate(BindingValue<TSource> value);
        protected abstract TValue GetDefaultValue(Type ownerType);

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

        private void SetValue(BindingValue<TValue> value)
        {
            static void Execute(BindingEntryBase<TValue, TSource> instance, BindingValue<TValue> value)
            {
                if (instance.Frame.Owner is not { } valueStore)
                    return;

                var owner = valueStore.Owner;
                var property = instance.Property;
                var originalType = value.Type;

                if (!value.HasValue && value.Type != BindingValueType.DataValidationError)
                    value = value.WithValue(instance.GetCachedDefaultValue());

                if (instance._uncommon?._hasDataValidation == true)
                {
                    instance._uncommon._dataValidationState = value.Type;
                    instance._uncommon._dataValidationError = value.Error;
                }

                if (value.HasValue &&
                    (!instance._hasValue || !EqualityComparer<TValue>.Default.Equals(instance._value, value.Value)))
                {
                    instance._value = value.Value;
                    instance._hasValue = true;
                    if (instance._subscription is not null && instance._subscription != s_creatingQuiet)
                        instance.Frame.Owner?.OnBindingValueChanged(instance, instance.Frame.Priority);
                }
            }

            if (value.Type == BindingValueType.DoNothing)
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

        private void BindingCompleted()
        {
            _subscription = null;
            Frame.OnBindingCompleted(this);
        }

        private TValue GetCachedDefaultValue()
        {
            if (_uncommon?._isDefaultValueInitialized != true)
            {
                _uncommon ??= new();
                _uncommon._defaultValue = GetDefaultValue(Frame.Owner!.Owner.GetType());
                _uncommon._isDefaultValueInitialized = true;
            }

            return _uncommon._defaultValue!;
        }

        private class UncommonFields
        {
            public TValue? _defaultValue;
            public bool _isDefaultValueInitialized;
            public bool _hasDataValidation;
            public BindingValueType _dataValidationState;
            public Exception? _dataValidationError;
        }
    }
}
