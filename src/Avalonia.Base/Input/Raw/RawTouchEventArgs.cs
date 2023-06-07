using System;
using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    [PrivateApi]
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
    }
}
