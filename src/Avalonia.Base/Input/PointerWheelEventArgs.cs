namespace Avalonia.Input
{
    public class PointerWheelEventArgs : PointerEventArgs
    {
        public Vector Delta { get; }

        public PointerWheelEventArgs(object? source, IPointer pointer, Visual rootVisual,
            Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, Vector delta)
            : base(InputElement.PointerWheelChangedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
