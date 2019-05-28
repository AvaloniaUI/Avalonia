namespace Avalonia.Input.Raw
{
    public class RawTouchEventArgs : RawMouseEventArgs
    {
        public RawTouchEventArgs(IInputDevice device, ulong timestamp, IInputRoot root,
            RawMouseEventType type, Point position, InputModifiers inputModifiers,
            long touchPointId) 
            : base(device, timestamp, root, type, position, inputModifiers)
        {
            TouchPointId = touchPointId;
        }

        public long TouchPointId { get; set; }
    }
}
