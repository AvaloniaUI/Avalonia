using System;

namespace Avalonia.Platform
{
    /// <summary>
    /// Represents a platform-specific handle.
    /// </summary>
    public class PlatformHandle : IPlatformHandle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformHandle"/> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="descriptor">
        /// An optional string that describes what <paramref name="handle"/> represents.
        /// </param>
        public PlatformHandle(IntPtr handle, string? descriptor)
        {
            Handle = handle;
            HandleDescriptor = descriptor;
        }

        /// <summary>
        /// Gets the handle.
        /// </summary>
        public IntPtr Handle { get; }

        /// <summary>
        /// Gets an optional string that describes what <see cref="Handle"/> represents.
        /// </summary>
        public string? HandleDescriptor { get; }
    }
}
