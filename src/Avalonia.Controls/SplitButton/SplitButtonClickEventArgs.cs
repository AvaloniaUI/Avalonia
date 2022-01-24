using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides event data for the <see cref="SplitButton.Click"/> event.
    /// </summary>
    public class SplitButtonClickEventArgs : RoutedEventArgs
    {
        public SplitButtonClickEventArgs()
        {
        }

        public SplitButtonClickEventArgs(RoutedEvent? routedEvent)
        {
            RoutedEvent = routedEvent;
        }

        public SplitButtonClickEventArgs(RoutedEvent? routedEvent, IInteractive? source)
        {
            RoutedEvent = routedEvent;
            Source = source;
        }
    }
}
