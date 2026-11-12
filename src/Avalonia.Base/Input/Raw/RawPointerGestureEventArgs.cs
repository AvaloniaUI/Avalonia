using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    [PrivateApi]
    public class RawPointerGestureEventArgs : RawPointerEventArgs
    {
        public RawPointerGestureEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            RawPointerEventType gestureType,
            Point position,
            Vector delta, RawInputModifiers inputModifiers,
            TouchPadGesturePhase phase)
            : base(device, timestamp, root, gestureType, position, inputModifiers)
        {
            Delta = delta;
            Phase = phase;
        }

        public Vector Delta { get; private set; }

        public TouchPadGesturePhase Phase { get; private set; }
    }
}
