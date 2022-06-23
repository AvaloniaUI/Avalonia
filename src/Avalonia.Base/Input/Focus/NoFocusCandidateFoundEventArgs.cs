using Avalonia.Interactivity;

namespace Avalonia.Input
{
    /// <summary>
    /// Provides data for the <see cref="InputElement.NoFocusCandidateFound"/> event
    /// </summary>
    public class NoFocusCandidateFoundEventArgs : RoutedEventArgs
    {
        internal NoFocusCandidateFoundEventArgs(NavigationDirection direction, FocusInputDeviceKind inputDeviceKind)
        {
            Direction = direction;
            InputDevice = inputDeviceKind;
        }

        /// <summary>
        /// Gets the focus move direction
        /// </summary>
        public NavigationDirection Direction { get; }

        /// <summary>
        /// Gets the input device type from which input events are received
        /// </summary>
        public FocusInputDeviceKind InputDevice { get; }
    }
}
