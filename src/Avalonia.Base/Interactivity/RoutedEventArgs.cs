using System;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Provides state information and data specific to a routed event.
    /// </summary>
    public class RoutedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedEventArgs"/> class.
        /// </summary>
        public RoutedEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        public RoutedEventArgs(RoutedEvent? routedEvent)
        {
            RoutedEvent = routedEvent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        /// <param name="source">The source object that raised the routed event.</param>
        public RoutedEventArgs(RoutedEvent? routedEvent, object? source)
        {
            RoutedEvent = routedEvent;
            Source = source;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the routed event has already been handled.
        /// </summary>
        /// <remarks>
        /// Once handled, a routed event should be ignored.
        /// </remarks>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets or sets the routed event associated with these event args.
        /// </summary>
        public RoutedEvent? RoutedEvent { get; set; }

        /// <summary>
        /// Gets or sets the routing strategy (direct, bubbling, or tunneling) of the routed event.
        /// </summary>
        public RoutingStrategies Route { get; set; }

        /// <summary>
        /// Gets or sets the source object that raised the routed event.
        /// </summary>
        public object? Source { get; set; }
    }
}
