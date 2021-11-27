using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerTouchPadGestureRotateEventArgs : PointerEventArgs
    {
        public double Delta { get; set; }

        public PointerTouchPadGestureRotateEventArgs(IInteractive source, IPointer pointer, IVisual rootVisual,
            Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, double delta) 
            : base(InputElement.PointerTouchPadGestureRotateEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
