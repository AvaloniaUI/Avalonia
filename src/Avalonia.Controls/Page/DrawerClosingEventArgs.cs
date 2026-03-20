using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="DrawerPage.ClosingEvent"/> routed event.
    /// </summary>
    public class DrawerClosingEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DrawerClosingEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event data.</param>
        public DrawerClosingEventArgs(RoutedEvent routedEvent) : base(routedEvent) { }

        /// <summary>
        /// Gets or sets a value indicating whether the closing should be cancelled.
        /// </summary>
        public bool Cancel { get; set; }
    }
}
