// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.


namespace Avalonia.Input.Raw
{
    public class RawMouseWheelEventArgs : RawPointerEventArgs
    {
        public RawMouseWheelEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            Point position,
            Vector delta, InputModifiers inputModifiers)
            : base(device, timestamp, root, RawPointerEventType.Wheel, position, inputModifiers)
        {
            Delta = delta;
        }

        public Vector Delta { get; private set; }
    }
}
