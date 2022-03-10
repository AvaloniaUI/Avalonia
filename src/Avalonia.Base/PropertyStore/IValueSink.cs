using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents an entity that can receive change notifications in a <see cref="ValueStore"/>.
    /// </summary>
    internal interface IValueSink
    {
        void ValueChanged<T>(AvaloniaPropertyChangedEventArgs<T> change);

        void Completed<T>(
            StyledPropertyBase<T> property,
            IPriorityValueEntry entry,
            Optional<T> oldValue);
    }
}
