using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class PointerDeltaEventArgs : PointerEventArgs
    {
        /// <summary>
        /// Gets the amount of change reported by the touchpad gesture since the previous event.
        /// </summary>
        /// <remarks>
        /// The meaning depends on the gesture:
        /// <list type="bullet">
        /// <item>Magnify: the change in scale factor, in both X and Y. Positive values zoom in.</item>
        /// <item>Rotate: the change in rotation in degrees, in both X and Y. Positive values are counterclockwise.</item>
        /// <item>Swipe: the direction of the swipe on each axis.</item>
        /// </list>
        /// </remarks>
        public Vector Delta { get; }

        /// <summary>
        /// Gets the phase of the touchpad gesture this event belongs to.
        /// </summary>
        public TouchPadGesturePhase Phase { get; }

        // TODO13: merge with the constructor below
        public PointerDeltaEventArgs(RoutedEvent? routedEvent, object? source,
            IPointer pointer, Visual rootVisual, Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, Vector delta)
            : this(routedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers, delta, TouchPadGesturePhase.None)
        {
        }

        public PointerDeltaEventArgs(RoutedEvent? routedEvent, object? source,
            IPointer pointer, Visual rootVisual, Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, Vector delta, TouchPadGesturePhase phase)
            : base(routedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
            Phase = phase;
        }
    }
}
