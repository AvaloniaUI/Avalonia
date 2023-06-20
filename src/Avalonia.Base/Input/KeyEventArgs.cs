using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class KeyEventArgs : RoutedEventArgs
    {
        public Key Key { get; init; }

        public KeyModifiers KeyModifiers { get; init; }
    }
}
