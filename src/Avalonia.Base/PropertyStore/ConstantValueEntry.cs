using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents an untyped interface to <see cref="ConstantValueEntry{T}"/>.
    /// </summary>
    internal interface IConstantValueEntry : IPriorityValueEntry, IDisposable
    {
    }

    /// <summary>
    /// Stores a value with a priority in a <see cref="ValueStore"/> or
    /// <see cref="PriorityValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    internal class ConstantValueEntry<T> : IPriorityValueEntry<T>, IConstantValueEntry
    {
        private IValueSink _sink;
        private Optional<T> _value;

        public ConstantValueEntry(
            StyledPropertyBase<T> property,
            [AllowNull] T value,
            BindingPriority priority,
            IValueSink sink)
        {
            Property = property;
            _value = value;
            Priority = priority;
            _sink = sink;
        }

        public StyledPropertyBase<T> Property { get; }
        public BindingPriority Priority { get; private set; }
        Optional<object> IValue.GetValue() => _value.ToObject();

        public Optional<T> GetValue(BindingPriority maxPriority = BindingPriority.Animation)
        {
            return Priority >= maxPriority ? _value : Optional<T>.Empty;
        }

        public void Dispose()
        {
            var oldValue = _value;
            _value = default;
            Priority = BindingPriority.Unset;
            _sink.Completed(Property, this, oldValue);
        }

        public void Reparent(IValueSink sink) => _sink = sink;
        public void Start() { }

        public void RaiseValueChanged(
            IValueSink sink,
            IAvaloniaObject owner,
            AvaloniaProperty property,
            Optional<object> oldValue,
            Optional<object> newValue)
        {
            sink.ValueChanged(new AvaloniaPropertyChangedEventArgs<T>(
                owner,
                (AvaloniaProperty<T>)property,
                oldValue.Cast<T>(),
                newValue.Cast<T>(),
                Priority));
        }
    }
}
