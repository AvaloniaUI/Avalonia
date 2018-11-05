using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class TextInputHandlerSelectionEventArgs : RoutedEventArgs
    {
        public ITextInputHandler Handler { get; set; }
    }
}
