using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data specific to a <see cref="TextBox.TextChanged"/> event.
    /// </summary>
    public class TextChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextChangedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        public TextChangedEventArgs(RoutedEvent? routedEvent)
            : base (routedEvent)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextChangedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        /// <param name="source">The source object that raised the routed event.</param>
        public TextChangedEventArgs(RoutedEvent? routedEvent, Interactive? source)
            : base(routedEvent, source)
        {
        }
    }
}
