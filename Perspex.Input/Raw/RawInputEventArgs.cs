// -----------------------------------------------------------------------
// <copyright file="RawInputEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input.Raw
{
    using System;

    public class RawInputEventArgs : EventArgs
    {
        public RawInputEventArgs(IInputDevice device, uint timestamp)
        {
            Contract.Requires<ArgumentNullException>(device != null);

            this.Device = device;
            this.Timestamp = timestamp;
        }

        public IInputDevice Device { get; private set; }

        public uint Timestamp { get; private set; }
    }
}
