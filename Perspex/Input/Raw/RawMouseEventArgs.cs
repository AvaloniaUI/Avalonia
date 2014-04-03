// -----------------------------------------------------------------------
// <copyright file="RawMouseEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input.Raw
{
    using System;
    using Perspex.Layout;

    public enum RawMouseEventType
    {
        Move,
        LeftButtonDown,
        LeftButtonUp,
    }

    public class RawMouseEventArgs : RawInputEventArgs
    {
        public RawMouseEventArgs(
            IInputDevice device,
            ILayoutRoot root,
            RawMouseEventType type,
            Point position)
            : base(device)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            this.Root = root;
            this.Position = position;
            this.Type = type;
        }

        public ILayoutRoot Root { get; private set; }

        public Point Position { get; private set; }

        public RawMouseEventType Type { get; private set; }
    }
}
