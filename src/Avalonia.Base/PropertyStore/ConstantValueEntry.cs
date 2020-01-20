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
    internal class ConstantValueEntry<T> : IPriorityValueEntry<T>
    {
        public ConstantValueEntry(
            StyledPropertyBase<T> property,
            T value,
            BindingPriority priority)
        {
            Property = property;
            Value = value;
            Priority = priority;
        }

        public StyledPropertyBase<T> Property { get; }
        public BindingPriority Priority { get; }
        public Optional<T> Value { get; }
        Optional<object> IValue.Value => Value.ToObject();
        BindingPriority IValue.ValuePriority => Priority;

        public void Reparent(IValueSink sink) { }
    }
}
