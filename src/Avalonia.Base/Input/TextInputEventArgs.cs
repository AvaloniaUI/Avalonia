using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class TextInputEventArgs : RoutedEventArgs
    {
        internal TextInputEventArgs()
        {

        }
        public IKeyboardDevice? Device { get; set; }

        public string? Text { get; set; }
    }
}
