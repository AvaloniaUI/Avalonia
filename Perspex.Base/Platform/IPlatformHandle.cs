// -----------------------------------------------------------------------
// <copyright file="Window.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;

    /// <summary>
    /// Represents a platform-specific handle.
    /// </summary>
    public interface IPlatformHandle
    {
        /// <summary>
        /// Gets the handle.
        /// </summary>
        IntPtr Handle { get; }

        /// <summary>
        /// Gets an optional string that describes what <see cref="Handle"/> represents.
        /// </summary>
        string HandleDescriptor { get; }
    }
}
