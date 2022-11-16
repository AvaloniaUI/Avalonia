using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerWheelEventArgs : PointerEventArgs
    {
        public Vector Delta { get; set; }
        public Vector RawDelta { get; set; }

        internal PointerWheelEventArgs(IInteractive source, IPointer pointer, IVisual rootVisual,
            Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, Vector delta, Vector rawDelta)
            : base(InputElement.PointerWheelChangedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
            RawDelta = rawDelta;
        }
    }
}
