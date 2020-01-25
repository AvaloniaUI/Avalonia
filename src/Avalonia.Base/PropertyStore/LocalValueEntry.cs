using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Stores a value with local value priority in a <see cref="ValueStore"/> or
    /// <see cref="PriorityValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
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
