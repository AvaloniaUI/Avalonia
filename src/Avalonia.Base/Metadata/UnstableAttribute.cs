using System;

namespace Avalonia.Metadata
{
    /// <summary>
    /// This API is unstable and is not covered by API compatibility guarantees between minor and
    /// patch releases.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class UnstableAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnstableAttribute"/> class.
        /// </summary>
        public UnstableAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnstableAttribute"/> class.
        /// </summary>
        /// <param name="message">The text string that describes alternative workarounds.</param>
        public UnstableAttribute(string? message)
        {
            Message = message;
        }

        /// <summary>
        /// Gets a value that indicates whether the compiler will treat usage of the obsolete program element as an error.
        /// </summary>
        public string? Message { get; }
    }
}
