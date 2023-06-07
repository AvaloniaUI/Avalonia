using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data specific to a <see cref="TextBox.TextChanging"/> event.
    /// </summary>
    public class TextChangingEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextChangingEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        public TextChangingEventArgs(RoutedEvent? routedEvent)
            : base (routedEvent)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextChangingEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        /// <param name="source">The source object that raised the routed event.</param>
        public TextChangingEventArgs(RoutedEvent? routedEvent, Interactive? source)
            : base(routedEvent, source)
        {
        }
    }
}
