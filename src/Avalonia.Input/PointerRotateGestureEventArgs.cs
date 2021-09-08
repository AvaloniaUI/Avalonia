using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerRotateGestureEventArgs : PointerEventArgs
    {
        public double Delta { get; set; }

        public PointerRotateGestureEventArgs(IInteractive source, IPointer pointer, IVisual rootVisual,
            Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, double delta) 
            : base(InputElement.PointerRotateGestureEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
