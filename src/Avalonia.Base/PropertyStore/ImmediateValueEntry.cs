using System;

namespace Avalonia.PropertyStore
{
    internal class ImmediateValueEntry<T> : IValueEntry<T>, IDisposable
    {
        private readonly ImmediateValueFrame _owner;
        private readonly T _value;

        public ImmediateValueEntry(
            ImmediateValueFrame owner,
            StyledProperty<T> property, 
            T value)
        {
            _owner = owner;
            _value = value;
            Property = property;
        }

        public StyledProperty<T> Property { get; }
        public bool HasValue => true;
        AvaloniaProperty IValueEntry.Property => Property;

        public void Unsubscribe() { }

        public void Dispose() => _owner.OnEntryDisposed(this);

        object? IValueEntry.GetValue() => _value;
        T IValueEntry<T>.GetValue() => _value;
    }
}
