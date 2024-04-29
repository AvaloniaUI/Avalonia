using System;
using Avalonia.Data;

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
        AvaloniaProperty IValueEntry.Property => Property;

        public void Unsubscribe() { }

        public void Dispose() => _owner.OnEntryDisposed(this);

        bool IValueEntry.HasValue() => true;
        object? IValueEntry.GetValue() => _value;
        T IValueEntry<T>.GetValue() => _value;

        bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
        {
            state = BindingValueType.Value;
            error = null;
            return false;
        }
    }
}
