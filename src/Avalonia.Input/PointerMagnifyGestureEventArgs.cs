using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerMagnifyGestureEventArgs : PointerEventArgs
    {
        public double Delta { get; set; }

        public PointerMagnifyGestureEventArgs(IInteractive source, IPointer pointer, IVisual rootVisual,
            Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, double delta) 
            : base(InputElement.PointerMagnifyGestureEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
