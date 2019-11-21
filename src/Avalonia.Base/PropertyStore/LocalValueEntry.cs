using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal class LocalValueEntry<T> : IValue<T>
    {
        private T _value;

        public LocalValueEntry(T value) => _value = value;
        public Optional<T> Value => _value;
        public BindingPriority ValuePriority => BindingPriority.LocalValue;
        Optional<object> IValue.Value => Value.ToObject();
        public void SetValue(T value) => _value = value;
    }
}
