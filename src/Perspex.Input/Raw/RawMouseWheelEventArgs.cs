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
            uint timestamp,
            IInputRoot root,
            Point position,
            Vector delta, ModifierKeys modifierKeys)
            : base(device, timestamp, root, RawMouseEventType.Wheel, position, modifierKeys)
        {
            this.Delta = delta;
        }

        public Vector Delta { get; private set; }
    }
}
