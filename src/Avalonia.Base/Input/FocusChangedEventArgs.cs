using Avalonia.Interactivity;

namespace Avalonia.Input
{
    /// <summary>
    /// Represents the arguments of <see cref="InputElement.GotFocus"/> and <see cref="InputElement.LostFocus"/>.
    /// </summary>
    public class FocusChangedEventArgs : RoutedEventArgs, IKeyModifiersEventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FocusChangedEventArgs"/>.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        public FocusChangedEventArgs(RoutedEvent routedEvent)
            : base(routedEvent)
        {
        }

        /// <summary>
        /// Gets or sets the element that focus has moved to.
        /// </summary>
        public IInputElement? NewFocusedElement { get; init; }

        /// <summary>
        /// Gets or sets the element that previously had focus.
        /// </summary>
        public IInputElement? OldFocusedElement { get; init; }

        /// <summary>
        /// Gets or sets a value indicating how the change in focus occurred.
        /// </summary>
        public NavigationMethod NavigationMethod { get; init; }

        /// <summary>
        /// Gets or sets any key modifiers active at the time of focus.
        /// </summary>
        public KeyModifiers KeyModifiers { get; init; }
    }
}
