// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Input.Raw
{
    public enum RawMouseEventType
    {
        LeaveWindow,
        LeftButtonDown,
        LeftButtonUp,
        RightButtonDown,
        RightButtonUp,
        MiddleButtonDown,
        MiddleButtonUp,
        Move,
        Wheel,
    }

    public class RawMouseEventArgs : RawInputEventArgs
    {
        public RawMouseEventArgs(
            IInputDevice device,
            uint timestamp,
            IInputRoot root,
            RawMouseEventType type,
            Point position, InputModifiers inputModifiers)
            : base(device, timestamp)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            Root = root;
            Position = position;
            Type = type;
            InputModifiers = inputModifiers;
        }

        public IInputRoot Root { get; private set; }

        public Point Position { get; set; }

        public RawMouseEventType Type { get; private set; }

        public InputModifiers InputModifiers { get; private set; }
    }
}
