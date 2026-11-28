namespace Avalonia.Input
{
    /// <summary>
    /// Provides data for the <see cref="InputElement.PointerWheelGestureEndedEvent"/> event.
    /// </summary>
    public class PointerWheelGestureEndedEventArgs(
        object? source,
        IPointer pointer,
        Visual? rootVisual,
        Point rootVisualPosition,
        ulong timestamp,
        PointerPointProperties properties,
        KeyModifiers modifiers)
        : PointerEventArgs(
            InputElement.PointerWheelGestureEndedEvent,
            source,
            pointer,
            rootVisual,
            rootVisualPosition,
            timestamp,
            properties,
            modifiers)
    {
        /// <summary>
        /// Gets whether the event belongs to the inertial part of a scroll (after the user lifted their fingers).
        /// </summary>
        public bool IsInertial { get; init; }

        /// <summary>
        /// Gets a value indicating whether the gesture was canceled by the system instead of being completed by the user.
        /// </summary>
        public bool IsCanceled { get; init; }
    }
}
