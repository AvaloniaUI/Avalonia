using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data specific to a TextChanged event.
    /// </summary>
    public class TextChangedEventArgs : RoutedEventArgs
    {
        public TextChangedEventArgs(RoutedEvent? routedEvent)
            : base (routedEvent)
        {
        }

        public TextChangedEventArgs(RoutedEvent? routedEvent, Interactive? source)
            : base(routedEvent, source)
        {
        }
    }
}
