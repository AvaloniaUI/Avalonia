using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents a union type of <see cref="ValueStore"/> and <see cref="PriorityValue{T}"/>,
    /// which are the valid owners of a value store <see cref="IValue"/>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    internal readonly struct ValueOwner<T>
    {
        private readonly ValueStore? _store;
        private readonly PriorityValue<T>? _priorityValue;

        public ValueOwner(ValueStore o)
        {
            _store = o;
            _priorityValue = null;
        }

        public ValueOwner(PriorityValue<T> v)
        {
            _store = null;
            _priorityValue = v;
        }

        public bool IsValueStore => _store is not null;

        public void Completed(StyledPropertyBase<T> property, IPriorityValueEntry entry, Optional<T> oldValue)
        {
            if (_store is not null)
                _store?.Completed(property, entry, oldValue);
            else
                _priorityValue!.Completed(entry, oldValue);
        }

        public void ValueChanged(AvaloniaPropertyChangedEventArgs<T> e)
        {
            if (_store is not null)
                _store?.ValueChanged(e);
            else
                _priorityValue!.ValueChanged(e);
        }
    }
}
