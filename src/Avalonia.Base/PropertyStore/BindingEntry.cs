using System;
using Avalonia.Data;
using Avalonia.Threading;

#nullable enable

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents an untyped interface to <see cref="BindingEntry{T}"/>.
    /// </summary>
    internal interface IBindingEntry : IPriorityValueEntry, IDisposable
    {
    }

    /// <summary>
    /// Stores a binding in a <see cref="ValueStore"/> or <see cref="PriorityValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    internal class BindingEntry<T> : IBindingEntry, IPriorityValueEntry<T>, IObserver<BindingValue<T>>
    {
        private readonly IAvaloniaObject _owner;
        private IValueSink _sink;
        private IDisposable? _subscription;
        private Optional<T> _value;

        public BindingEntry(
            IAvaloniaObject owner,
            StyledPropertyBase<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority,
            IValueSink sink)
        {
            _owner = owner;
            Property = property;
            Source = source;
            Priority = priority;
            _sink = sink;
        }

        public StyledPropertyBase<T> Property { get; }
        public BindingPriority Priority { get; }
        public IObservable<BindingValue<T>> Source { get; }
        Optional<object> IValue.GetValue() => _value.ToObject();

        public Optional<T> GetValue(BindingPriority maxPriority)
        {
            return Priority >= maxPriority ? _value : Optional<T>.Empty;
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            _sink.Completed(Property, this, _value);
        }

        public void OnCompleted() => _sink.Completed(Property, this, _value);

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(BindingValue<T> value)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                UpdateValue(value); 
            }
            else
            {
                // To avoid allocating closure in the outer scope we need to capture variables
                // locally. This allows us to skip most of the allocations when on UI thread.
                var instance = this;
                var newValue = value;

                Dispatcher.UIThread.Post(() => instance.UpdateValue(newValue));
            }
        }

        public void Start()
        {
            _subscription = Source.Subscribe(this);
        }

        public void Reparent(IValueSink sink) => _sink = sink;
        
        private void UpdateValue(BindingValue<T> value)
        {
            if (value.HasValue && Property.ValidateValue?.Invoke(value.Value) == false)
            {
                value = Property.GetDefaultValue(_owner.GetType());
            }

            if (value.Type == BindingValueType.DoNothing)
            {
                return;
            }

            var old = _value;

            if (value.Type != BindingValueType.DataValidationError)
            {
                _value = value.ToOptional();
            }

            _sink.ValueChanged(new AvaloniaPropertyChangedEventArgs<T>(_owner, Property, old, value, Priority));
        }
    }
}
