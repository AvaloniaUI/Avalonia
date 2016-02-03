// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Layout;

namespace Perspex.Input.Raw
{
    public class RawMouseWheelEventArgs : RawMouseEventArgs
    {
        public RawMouseWheelEventArgs(
            IInputDevice device,
            IInputRoot root,
            uint timestamp,
            Point position,
            Vector delta, InputModifiers inputModifiers)
            : base(device, root, timestamp, RawMouseEventType.Wheel, position, inputModifiers)
        {
            Delta = delta;
        }

        public Vector Delta { get; private set; }
    }
}
