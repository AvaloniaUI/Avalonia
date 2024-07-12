using System;

namespace Avalonia.Platform
{
    /// <summary>
    /// Represents a platform-specific handle.
    /// </summary>
    public class PlatformHandle : IPlatformHandle, IEquatable<PlatformHandle>
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"PlatformHandle {{ {HandleDescriptor} = {Handle} }}";
        }

        /// <inheritdoc/>
        public bool Equals(PlatformHandle? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Handle == other.Handle && HandleDescriptor == other.HandleDescriptor;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PlatformHandle)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Handle, HandleDescriptor).GetHashCode();
        }

        public static bool operator ==(PlatformHandle? left, PlatformHandle? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PlatformHandle? left, PlatformHandle? right)
        {
            return !Equals(left, right);
        }
    }
}
