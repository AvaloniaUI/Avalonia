using System;

namespace Avalonia.PropertyStore
{
    internal class ImmediateValueEntry<T> : IValueEntry, IDisposable
    {
        private readonly ImmediateValueFrame _owner;
        private readonly T _value;

        public ImmediateValueEntry(
            ImmediateValueFrame owner,
            StyledPropertyBase<T> property, 
            T value)
        {
            _owner = owner;
            _value = value;
            Property = property;
        }

        public StyledPropertyBase<T> Property { get; }
        public bool HasValue => true;
        AvaloniaProperty IValueEntry.Property => Property;

        public bool TryGetValue(out object? value)
        {
            value = _value;
            return true;
        }

        public void Unsubscribe() { }

        public void Dispose() => _owner.OnEntryDisposed(this);

        object? IValueEntry.GetValue() => _value;
    }
}
