namespace Avalonia.Interactivity
{
    /// <summary>
    /// Provides state information and data specific to a cancelable routed event.
    /// </summary>
    public class CancelRoutedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CancelRoutedEventArgs"/> class.
        /// </summary>
        public CancelRoutedEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelRoutedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        public CancelRoutedEventArgs(RoutedEvent? routedEvent)
            : base(routedEvent)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelRoutedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        /// <param name="source">The source object that raised the routed event.</param>
        public CancelRoutedEventArgs(RoutedEvent? routedEvent, object? source)
            : base(routedEvent, source)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the routed event should be canceled.
        /// </summary>
        public bool Cancel { get; set; } = false;
    }
}
