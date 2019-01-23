// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Input.Raw
{
    /// <summary>
    /// A raw input event.
    /// </summary>
    /// <remarks>
    /// Raw input events are sent from the windowing subsystem to the <see cref="InputManager"/>
    /// for processing: this gives an application the opportunity to pre-process the event. After
    /// pre-processing they are consumed by the relevant <see cref="Device"/> and turned into
    /// standard Avalonia events.
    /// </remarks>
    public class RawInputEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RawInputEventArgs"/> class.
        /// </summary>
        /// <param name="device">The associated device.</param>
        /// <param name="timestamp">The event timestamp.</param>
        public RawInputEventArgs(IInputDevice device, ulong timestamp)
        {
            Contract.Requires<ArgumentNullException>(device != null);

            Device = device;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Gets the associated device.
        /// </summary>
        public IInputDevice Device { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the event was handled.
        /// </summary>
        /// <remarks>
        /// If an event is not marked handled after processing via the
        /// <see cref="InputManager"/>, then it will be passed on to the underlying OS for
        /// handling.
        /// </remarks>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets the timestamp associated with the event.
        /// </summary>
        public ulong Timestamp { get; private set; }
    }
}
