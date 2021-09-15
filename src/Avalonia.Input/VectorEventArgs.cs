using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class VectorEventArgs : RoutedEventArgs
    {
        public VectorEventArgs(RoutedEvent routedEvent,
            KeyModifiers modifiers,
            Vector vector)
        {
            RoutedEvent = routedEvent;
            KeyModifiers = modifiers;
            Vector = vector;
        }
        
        public Vector Vector { get; }
        public KeyModifiers KeyModifiers { get; }
    }
}
