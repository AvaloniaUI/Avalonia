using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents an entity that can receive change notifications in a <see cref="ValueStore"/>.
    /// </summary>
    internal interface IValueSink
    {
        void ValueChanged<T>(
            StyledPropertyBase<T> property,
            BindingPriority priority,
            Optional<T> oldValue,
            BindingValue<T> newValue);

        void Completed<T>(
            StyledPropertyBase<T> property,
            IPriorityValueEntry entry,
            Optional<T> oldValue);
    }
}
