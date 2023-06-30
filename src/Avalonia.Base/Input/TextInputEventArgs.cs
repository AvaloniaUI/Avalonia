using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class TextInputEventArgs : RoutedEventArgs
    {
        public string? Text { get; set; }
    }
}
