using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    [PrivateApi]
    public class RawMouseWheelEventArgs : RawPointerEventArgs
    {
        public RawMouseWheelEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            Point position,
            Vector delta, RawInputModifiers inputModifiers)
            : base(device, timestamp, root, RawPointerEventType.Wheel, position, inputModifiers)
        {
            Delta = delta;
        }

        public Vector Delta { get; private set; }

        /// <summary>
        /// Gets the phase of the scroll gesture, or of its inertial part if <see cref="IsInertial"/> is true.
        /// </summary>
        public RawPointerGesturePhase Phase { get; init; }

        /// <summary>
        /// Gets whether the event belongs to the inertial part of a scroll (after the user lifted their fingers).
        /// </summary>
        public bool IsInertial { get; init; }
    }
}
