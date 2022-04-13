using System;

namespace Avalonia.Platform
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
        string? HandleDescriptor { get; }
    }
}
