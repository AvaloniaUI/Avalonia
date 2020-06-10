using System;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Stores a value with a priority in a <see cref="ValueStore"/> or
    /// <see cref="PriorityValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    internal class ConstantValueEntry<T> : IPriorityValueEntry<T>, IDisposable
    {
        private IValueSink _sink;
        private Optional<T> _value;

        public ConstantValueEntry(
            StyledPropertyBase<T> property,
            T value,
            BindingPriority priority,
            IValueSink sink)
        {
            Property = property;
            _value = value;
            Priority = priority;
            _sink = sink;
        }

        public StyledPropertyBase<T> Property { get; }
        public BindingPriority Priority { get; }
        Optional<object> IValue.GetValue() => _value.ToObject();

        public Optional<T> GetValue(BindingPriority maxPriority = BindingPriority.Animation)
        {
            return Priority >= maxPriority ? _value : Optional<T>.Empty;
        }

        public void Dispose() => _sink.Completed(Property, this, _value);
        public void Reparent(IValueSink sink) => _sink = sink;
    }
}
