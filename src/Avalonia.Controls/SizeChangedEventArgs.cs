using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Utilities;

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
        public SizeChangedEventArgs(RoutedEvent? routedEvent, object? source)
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
            object? source,
            Size previousSize,
            Size newSize)
            : base(routedEvent, source)
        {
            PreviousSize = previousSize;
            NewSize = newSize;
        }

        /// <summary>
        /// Gets a value indicating whether the height of the new size is considered
        /// different than the previous size height.
        /// </summary>
        /// <remarks>
        /// This will take into account layout epsilon and will not be true if both
        /// heights are considered equivalent for layout purposes. Remember there can
        /// be small variations in the calculations between layout cycles due to
        /// rounding and precision even when the size has not otherwise changed.
        /// </remarks>
        public bool HeightChanged => !MathUtilities.AreClose(NewSize.Height, PreviousSize.Height, LayoutHelper.LayoutEpsilon);

        /// <summary>
        /// Gets the new size (or bounds) of the object.
        /// </summary>
        public Size NewSize { get; init; }

        /// <summary>
        /// Gets the previous size (or bounds) of the object.
        /// </summary>
        public Size PreviousSize { get; init; }

        /// <summary>
        /// Gets a value indicating whether the width of the new size is considered
        /// different than the previous size width.
        /// </summary>
        /// <remarks>
        /// This will take into account layout epsilon and will not be true if both
        /// widths are considered equivalent for layout purposes. Remember there can
        /// be small variations in the calculations between layout cycles due to
        /// rounding and precision even when the size has not otherwise changed.
        /// </remarks>
        public bool WidthChanged => !MathUtilities.AreClose(NewSize.Width, PreviousSize.Width, LayoutHelper.LayoutEpsilon);
    }
}
