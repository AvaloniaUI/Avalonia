namespace Avalonia.Interactivity
{
    /// <summary>
    /// Provides both old and new property values with a routed event.
    /// </summary>
    /// <typeparam name="T">The type of values.</typeparam>
    public class RoutedPropertyChangedEventArgs<T> : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedPropertyChangedEventArgs{T}"/> class.
        /// </summary>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        public RoutedPropertyChangedEventArgs(T oldValue, T newValue, RoutedEvent? routedEvent)
            : base(routedEvent)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedPropertyChangedEventArgs{T}"/> class.
        /// </summary>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        /// <param name="source">The source object that raised the routed event.</param>
        public RoutedPropertyChangedEventArgs(T oldValue, T newValue, RoutedEvent? routedEvent, object? source)
            : base(routedEvent, source)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        public T OldValue { get; init; }

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        public T NewValue { get; init; }
    }
}
