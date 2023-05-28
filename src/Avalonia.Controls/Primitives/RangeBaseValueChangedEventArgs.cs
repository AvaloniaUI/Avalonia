using Avalonia.Interactivity;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Provides data specific to a <see cref="RangeBase.ValueChanged"/> event.
    /// </summary>
    public class RangeBaseValueChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RangeBaseValueChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldValue">The old value of the range value property.</param>
        /// <param name="newValue">The new value of the range value property.</param>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        public RangeBaseValueChangedEventArgs(double oldValue, double newValue, RoutedEvent? routedEvent)
            : base(routedEvent)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeBaseValueChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldValue">The old value of the range value property.</param>
        /// <param name="newValue">The new value of the range value property.</param>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        /// <param name="source">The source object that raised the routed event.</param>
        public RangeBaseValueChangedEventArgs(double oldValue, double newValue, RoutedEvent? routedEvent, object? source)
            : base(routedEvent, source)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the old value of the range value property.
        /// </summary>
        public double OldValue { get; init; }

        /// <summary>
        /// Gets the new value of the range value property.
        /// </summary>
        public double NewValue { get; init; }
    }
}
