using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class KeyEventArgs : RoutedEventArgs
    {
        public IKeyboardDevice? Device { get; set; }

        public Key Key { get; set; }

        public KeyModifiers KeyModifiers { get; set; }
    }
}
