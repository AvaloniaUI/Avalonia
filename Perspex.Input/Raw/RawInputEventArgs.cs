// -----------------------------------------------------------------------
// <copyright file="RawInputEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input.Raw
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Perspex.Layout;

    public class RawInputEventArgs : EventArgs
    {
        public RawInputEventArgs(IInputDevice device)
        {
            Contract.Requires<ArgumentNullException>(device != null);

            this.Device = device;
        }

        public IInputDevice Device { get; private set; }
    }
}
