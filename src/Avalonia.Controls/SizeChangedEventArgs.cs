using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data specific to a SizeChanged event.
    /// </summary>
    public class SizeChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SizeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        public SizeChangedEventArgs(RoutedEvent? routedEvent)
            : base (routedEvent)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        /// <param name="source">The source object that raised the routed event.</param>
        public SizeChangedEventArgs(RoutedEvent? routedEvent, IInteractive? source)
            : base(routedEvent, source)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        /// <param name="source">The source object that raised the routed event.</param>
        /// <param name="previousSize">The previous size (or bounds) of the object.</param>
        /// <param name="newSize">The new size (or bounds) of the object.</param>
        public SizeChangedEventArgs(
            RoutedEvent? routedEvent,
            IInteractive? source,
            Size previousSize,
            Size newSize)
            : base(routedEvent, source)
        {
            PreviousSize = previousSize;
            NewSize = newSize;
            HeightChanged = newSize.Height != previousSize.Height;
            WidthChanged = newSize.Width != previousSize.Width;
        }

        /// <summary>
        /// Gets a value indicating whether the height of the new size is different
        /// than the previous size height.
        /// </summary>
        public bool HeightChanged { get; init; }

        /// <summary>
        /// Gets the new size (or bounds) of the object.
        /// </summary>
        public Size NewSize { get; init; }

        /// <summary>
        /// Gets the previous size (or bounds) of the object.
        /// </summary>
        public Size PreviousSize { get; init; }

        /// <summary>
        /// Gets a value indicating whether the width of the new size is different
        /// than the previous size width.
        /// </summary>
        public bool WidthChanged { get; init; }
    }
}
