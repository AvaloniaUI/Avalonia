using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerTouchPadGestureMagnifyEventArgs : PointerEventArgs
    {
        public double Delta { get; set; }

        public PointerTouchPadGestureMagnifyEventArgs(IInteractive source, IPointer pointer, IVisual rootVisual,
            Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, double delta) 
            : base(InputElement.PointerTouchPadGestureMagnifyEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
