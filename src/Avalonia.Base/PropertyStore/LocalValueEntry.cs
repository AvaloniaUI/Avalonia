using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal class LocalValueEntry<T> : IValue<T>
    {
        public LocalValueEntry(T value) => Value = value;
        public Optional<T> Value { get; set; }
        public BindingPriority ValuePriority => BindingPriority.LocalValue;
        Optional<object> IValue.Value => Value.ToObject();
        
        public ConstantValueEntry<T> ToConstantValueEntry(StyledPropertyBase<T> property)
        {
            return new ConstantValueEntry<T>(property, Value.Value, BindingPriority.LocalValue);
        }
    }
}
