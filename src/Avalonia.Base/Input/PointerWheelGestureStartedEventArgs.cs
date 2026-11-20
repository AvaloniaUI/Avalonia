namespace Avalonia.Input
{
    /// <summary>
    /// Provides data for the <see cref="InputElement.PointerWheelGestureStartedEvent"/> event.
    /// </summary>
    /// <remarks>
    /// Until the matching <see cref="InputElement.PointerWheelGestureEndedEvent"/>, all the
    /// <see cref="InputElement.PointerWheelChangedEvent"/> events of the gesture are raised on the element
    /// that received this event, even if the pointer is no longer over it.
    /// </remarks>
    public class PointerWheelGestureStartedEventArgs(
        object? source,
        IPointer pointer,
        Visual rootVisual,
        Point rootVisualPosition,
        ulong timestamp,
        PointerPointProperties properties,
        KeyModifiers modifiers,
        bool isInertial)
        : PointerEventArgs(InputElement.PointerWheelGestureStartedEvent, source, pointer, rootVisual,
            rootVisualPosition,
            timestamp, properties, modifiers)
    {
        /// <summary>
        /// Gets whether the event belongs to the inertial part of a scroll (after the user lifted their fingers).
        /// </summary>
        public bool IsInertial { get; } = isInertial;
    }
}
