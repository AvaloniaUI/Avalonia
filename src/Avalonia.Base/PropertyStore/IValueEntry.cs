using System;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents an untyped value entry in a <see cref="ValueFrame"/>.
    /// </summary>
    internal interface IValueEntry
    {
        bool HasValue { get; }

        /// <summary>
        /// Gets the property that this value applies to.
        /// </summary>
        AvaloniaProperty Property { get; }

        /// <summary>
        /// Gets the value associated with the entry.
        /// </summary>
        /// <exception cref="AvaloniaInternalException">
        /// The entry has no value.
        /// </exception>
        object? GetValue();

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

        /// <summary>
        /// Called when the value entry is removed from the value store.
        /// </summary>
        void Unsubscribe();
    }
}
