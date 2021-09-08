using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerSwipeGestureEventArgs : PointerEventArgs
    {
        public Vector Delta { get; set; }

        public PointerSwipeGestureEventArgs(IInteractive source, IPointer pointer, IVisual rootVisual,
            Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, Vector delta) 
            : base(InputElement.PointerSwipeGestureEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
