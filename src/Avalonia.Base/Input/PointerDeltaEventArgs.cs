using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerDeltaEventArgs : PointerEventArgs
    {
        public Vector Delta { get; set; }

        internal PointerDeltaEventArgs(RoutedEvent routedEvent, IInteractive? source, 
            IPointer pointer, IVisual rootVisual, Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, Vector delta) 
            : base(routedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
