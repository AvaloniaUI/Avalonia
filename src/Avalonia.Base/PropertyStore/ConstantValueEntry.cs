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

        public ConstantValueEntry(
            StyledPropertyBase<T> property,
            T value,
            BindingPriority priority,
            IValueSink sink)
        {
            Property = property;
            Value = value;
            Priority = priority;
            _sink = sink;
        }

        public StyledPropertyBase<T> Property { get; }
        public BindingPriority Priority { get; }
        public Optional<T> Value { get; }
        Optional<object> IValue.Value => Value.ToObject();
        BindingPriority IValue.ValuePriority => Priority;

        public void Dispose() => _sink.Completed(Property, this, Value);
        public void Reparent(IValueSink sink) => _sink = sink;
    }
}
