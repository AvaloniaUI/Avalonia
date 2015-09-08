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
            Point position, ModifierKeys modifierKeys)
            : base(device, timestamp)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            Root = root;
            Position = position;
            Type = type;
            ModifierKeys = modifierKeys;
        }

        public IInputRoot Root { get; private set; }

        public Point Position { get; private set; }

        public RawMouseEventType Type { get; private set; }

        public ModifierKeys ModifierKeys { get; private set; }
    }
}
