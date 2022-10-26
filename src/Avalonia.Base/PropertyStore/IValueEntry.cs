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
        /// Called when the value entry is removed from the value store.
        /// </summary>
        void Unsubscribe();
    }
}
