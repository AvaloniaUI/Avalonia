using System;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
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
        public Optional<T> Value { get; private set; }
        Optional<object> IValue.Value => Value.ToObject();
        BindingPriority IValue.ValuePriority => Priority;

        public void Reparent(IValueSink sink) { }
    }
}
