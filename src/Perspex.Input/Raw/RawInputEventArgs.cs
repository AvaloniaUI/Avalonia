// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Input.Raw
{
    public class RawInputEventArgs : EventArgs
    {
        public RawInputEventArgs(
            IInputDevice device,
            IInputRoot root,
            uint timestamp)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            Device = device;
            Root = root;
            Timestamp = timestamp;
        }

        public IInputDevice Device { get; }

        public IInputRoot Root { get; }

        public uint Timestamp { get; }
    }
}
