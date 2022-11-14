namespace Avalonia.Input.Raw
{
    public class RawPointerGestureEventArgs : RawPointerEventArgs
    {
        public RawPointerGestureEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            RawPointerEventType gestureType,
            Point position,
            Vector delta, RawInputModifiers inputModifiers)
            : base(device, timestamp, root, gestureType, position, inputModifiers)
        {
            Delta = delta;
        }

        public Vector Delta { get; private set; }
    }
}
