// -----------------------------------------------------------------------
// <copyright file="RawMouseEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input.Raw
{
    using System;

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

            this.Root = root;
            this.Position = position;
            this.Type = type;
            this.ModifierKeys = modifierKeys;
        }

        public IInputRoot Root { get; private set; }

        public Point Position { get; private set; }

        public RawMouseEventType Type { get; private set; }

        public ModifierKeys ModifierKeys { get; private set; }
    }
}
