using System;

namespace Avalonia.Input.Raw
{
    public class RawTouchEventArgs : RawPointerEventArgs
    {
        public RawTouchEventArgs(IInputDevice device, ulong timestamp, IInputRoot root,
            RawPointerEventType type, Point position, RawInputModifiers inputModifiers,
            long rawPointerId) 
            : base(device, timestamp, root, type, position, inputModifiers)
        {
            RawPointerId = rawPointerId;
        }

        public RawTouchEventArgs(IInputDevice device, ulong timestamp, IInputRoot root,
            RawPointerEventType type, RawPointerPoint point, RawInputModifiers inputModifiers,
            long rawPointerId)
            : base(device, timestamp, root, type, point, inputModifiers)
        {
            RawPointerId = rawPointerId;
        }

        [Obsolete("Use RawPointerId")]
        public long TouchPointId { get => RawPointerId; set => RawPointerId = value; }
    }
}
