using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class PointerDeltaEventArgs : PointerEventArgs
    {
        public Vector Delta { get; }

        public PointerDeltaEventArgs(RoutedEvent? routedEvent, object? source,
            IPointer pointer, Visual rootVisual, Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, Vector delta) 
            : base(routedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
