using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal class BindingEntry<T> : IValueEntry<T>,
        IValueFrame,
        IObserver<T>,
        IObserver<BindingValue<T>>,
        IList<IValueEntry>,
        IDisposable
    {
        private readonly object _source;
        private IDisposable? _bindingSubscription;
        private ValueStore? _owner;
        private bool _hasValue;
        private T? _value;

        public BindingEntry(
            StyledPropertyBase<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority)
        {
            _source = source;
            Property = property;
            Priority = priority;
        }

        public BindingEntry(
            StyledPropertyBase<T> property,
            IObservable<T?> source,
            BindingPriority priority)
        {
            _source = source;
            Property = property;
            Priority = priority;
        }

        public bool HasValue
        {
            get
            {
                StartIfNecessary();
                return _hasValue;
            }
        }
        
        public bool IsActive => true;
        public BindingPriority Priority { get; }
        public StyledPropertyBase<T> Property { get; }
        AvaloniaProperty IValueEntry.Property => Property;
        public IList<IValueEntry> Values => this;
        int ICollection<IValueEntry>.Count => 1;
        bool ICollection<IValueEntry>.IsReadOnly => true;
        
        IValueEntry IList<IValueEntry>.this[int index] 
        { 
            get => this;
            set => throw new NotImplementedException(); 
        }

        public void Dispose()
        {
            _bindingSubscription?.Dispose();
            BindingCompleted();
        }

        public void SetOwner(ValueStore? owner) => _owner = owner;

        public bool TryGetValue(out T? value)
        {
            StartIfNecessary();
            value = _value;
            return _hasValue;
        }

        public bool TryGetValue(out object? value)
        {
            StartIfNecessary();
            value = _value;
            return _hasValue;
        }

        public void OnCompleted() => BindingCompleted();
        public void OnError(Exception error) => BindingCompleted();

        int IList<IValueEntry>.IndexOf(IValueEntry item) => throw new NotImplementedException();
        void IList<IValueEntry>.Insert(int index, IValueEntry item) => throw new NotImplementedException();
        void IList<IValueEntry>.RemoveAt(int index) => throw new NotImplementedException();
        void ICollection<IValueEntry>.Add(IValueEntry item) => throw new NotImplementedException();
        void ICollection<IValueEntry>.Clear() => throw new NotImplementedException();
        bool ICollection<IValueEntry>.Contains(IValueEntry item) => throw new NotImplementedException();
        void ICollection<IValueEntry>.CopyTo(IValueEntry[] array, int arrayIndex) => throw new NotImplementedException();
        bool ICollection<IValueEntry>.Remove(IValueEntry item) => throw new NotImplementedException();
        IEnumerator<IValueEntry> IEnumerable<IValueEntry>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        void IObserver<T>.OnNext(T value) => SetValue(value);

        void IObserver<BindingValue<T>>.OnNext(BindingValue<T> value)
        {
            if (value.HasValue)
                SetValue(value.Value);
            else
                ClearValue();
        }

        private void ClearValue()
        {
            _ = _owner ?? throw new AvaloniaInternalException("BindingEntry has no owner.");

            var oldValue = _hasValue ? new Optional<T>(_value) : default;

            if (_bindingSubscription is null)
                _owner.RemoveBindingEntry(this, oldValue);
            else if (_hasValue)
            {
                _hasValue = false;
                _value = default;
                _owner.ValueChanged(this, this, oldValue);
            }
        }

        private void SetValue(T? value)
        {
            _ = _owner ?? throw new AvaloniaInternalException("BindingEntry has no owner.");

            if (Property.ValidateValue?.Invoke(value) == false)
            {
                value = Property.GetDefaultValue(_owner.Owner.GetType());
            }

            if (!_hasValue || !EqualityComparer<T>.Default.Equals(_value, value))
            {
                var oldValue = _hasValue ? new Optional<T>(_value) : default;
                _value = value;
                _hasValue = true;

                // Only raise a property changed notifcation if we're not currently in the process of
                // starting the binding (in this case the value will be read immediately afterwards
                // and a notification raised).
                if (_bindingSubscription != Disposable.Empty)
                    _owner.ValueChanged(this, this, oldValue);
            }
        }

        private void StartIfNecessary()
        {
            if (_bindingSubscription is null)
            {
                // Prevent reentrancy by first assigning the subscription to a dummy
                // non-null value.
                _bindingSubscription = Disposable.Empty;

                if (_source is IObservable<BindingValue<T>> bv)
                    _bindingSubscription = bv.Subscribe(this);
                else if (_source is IObservable<T> b)
                    _bindingSubscription = b.Subscribe(this);
                else
                    throw new AvaloniaInternalException("Unexpected binding source.");
            }
        }

        private void BindingCompleted()
        {
            _bindingSubscription = null;
            ClearValue();
        }
    }
}
