// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Layout;

namespace Avalonia.Input.Raw
{
    public class RawMouseWheelEventArgs : RawMouseEventArgs
    {
        public RawMouseWheelEventArgs(
            IInputDevice device,
            uint timestamp,
            IInputRoot root,
            Point position,
            Vector delta, InputModifiers inputModifiers)
            : base(device, timestamp, root, RawMouseEventType.Wheel, position, inputModifiers)
        {
            Delta = delta;
        }

        public Vector Delta { get; private set; }
    }
}
