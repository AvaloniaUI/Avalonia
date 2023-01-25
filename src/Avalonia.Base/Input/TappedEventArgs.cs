using System;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class TappedEventArgs : RoutedEventArgs
    {
        private readonly PointerEventArgs lastPointerEventArgs;

        public TappedEventArgs(RoutedEvent routedEvent, PointerEventArgs lastPointerEventArgs)
            : base(routedEvent)
        {
            this.lastPointerEventArgs = lastPointerEventArgs;
        }

        public IPointer Pointer => lastPointerEventArgs.Pointer;
        public KeyModifiers KeyModifiers => lastPointerEventArgs.KeyModifiers;
        public ulong Timestamp => lastPointerEventArgs.Timestamp;
        
        public Point GetPosition(Visual? relativeTo) => lastPointerEventArgs.GetPosition(relativeTo);
    }
}
