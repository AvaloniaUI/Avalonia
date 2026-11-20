namespace Avalonia.Input
{
    public class PointerWheelEventArgs : PointerEventArgs
    {
        public Vector Delta { get; }

        /// <summary>
        /// Gets whether the event belongs to the inertial part of a scroll (after the user lifted their fingers).
        /// </summary>
        public bool IsInertial { get; init; }

        public PointerWheelEventArgs(object? source, IPointer pointer, Visual? rootVisual,
            Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, Vector delta)
            : base(InputElement.PointerWheelChangedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
