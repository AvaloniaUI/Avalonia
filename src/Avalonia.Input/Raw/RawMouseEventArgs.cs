// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Input.Raw
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
        NonClientLeftButtonDown,
    }

    /// <summary>
    /// A raw mouse event.
    /// </summary>
    public class RawMouseEventArgs : RawInputEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RawMouseEventArgs"/> class.
        /// </summary>
        /// <param name="device">The associated device.</param>
        /// <param name="timestamp">The event timestamp.</param>
        /// <param name="root">The root from which the event originates.</param>
        /// <param name="type">The type of the event.</param>
        /// <param name="position">The mouse position, in client DIPs.</param>
        /// <param name="inputModifiers">The input modifiers.</param>
        public RawMouseEventArgs(
            IInputDevice device,
            uint timestamp,
            IInputRoot root,
            RawMouseEventType type,
            Point position, 
            InputModifiers inputModifiers)
            : base(device, timestamp)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            Root = root;
            Position = position;
            Type = type;
            InputModifiers = inputModifiers;
        }

        /// <summary>
        /// Gets the root from which the event originates.
        /// </summary>
        public IInputRoot Root { get; }

        /// <summary>
        /// Gets the mouse position, in client DIPs.
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// Gets the type of the event.
        /// </summary>
        public RawMouseEventType Type { get; private set; }

        /// <summary>
        /// Gets the input modifiers.
        /// </summary>
        public InputModifiers InputModifiers { get; private set; }
    }
}
