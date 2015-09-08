// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Platform
{
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
