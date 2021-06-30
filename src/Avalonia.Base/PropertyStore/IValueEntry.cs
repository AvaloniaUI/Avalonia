#nullable enable

using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents an untyped value entry in an <see cref="IValueFrame"/>.
    /// </summary>
    internal interface IValueEntry
    {
        bool HasValue { get; }

        /// <summary>
        /// Gets the property that this value applies to.
        /// </summary>
        AvaloniaProperty Property { get; }

        /// <summary>
        /// Tries to get the value associated with the entry.
        /// </summary>
        /// <param name="value">
        /// When this method returns, contains the value associated with the entry if a value is
        /// present; otherwise, returns null.
        /// </param>
        /// <returns>
        /// true if the entry has an associated value; otherwise false.
        /// </returns>
        bool TryGetValue(out object? value);
    }
}
