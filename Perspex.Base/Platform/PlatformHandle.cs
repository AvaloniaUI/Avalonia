// -----------------------------------------------------------------------
// <copyright file="PlatformHandle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;

    public class PlatformHandle : IPlatformHandle
    {
        public PlatformHandle(IntPtr handle, string descriptor)
        {
            this.Handle = handle;
            this.HandleDescriptor = descriptor;
        }

        public IntPtr Handle { get; private set; }

        public string HandleDescriptor { get; private set; }
    }
}
