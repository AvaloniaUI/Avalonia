using Avalonia.Interactivity;

namespace Avalonia.Input
{
    /// <summary>
    /// Holds arguments for a <see cref="InputElement.GotFocusEvent"/>.
    /// </summary>
    public class GotFocusEventArgs : RoutedEventArgs
    {
        public GotFocusEventArgs() : base(InputElement.GotFocusEvent)
        {
        }

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
