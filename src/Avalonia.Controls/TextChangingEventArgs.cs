using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data specific to a TextChanging event.
    /// </summary>
    public class TextChangingEventArgs : RoutedEventArgs
    {
        public TextChangingEventArgs(RoutedEvent? routedEvent)
            : base (routedEvent)
        {
        }

        public TextChangingEventArgs(RoutedEvent? routedEvent, IInteractive? source)
            : base(routedEvent, source)
        {
        }
    }
}
