// -----------------------------------------------------------------------
// <copyright file="RawMouseWheelEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input.Raw
{
    using System;
    using Perspex.Layout;

    public class RawMouseWheelEventArgs : RawMouseEventArgs
    {
        public RawMouseWheelEventArgs(
            IInputDevice device,
            ILayoutRoot root,
            Point position,
            Vector delta)
            : base(device, root, RawMouseEventType.Wheel, position)
        {
            this.Delta = delta;
        }

        public Vector Delta { get; private set; }
    }
}
