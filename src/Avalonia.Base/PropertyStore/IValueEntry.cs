using System;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents an untyped value entry in a <see cref="ValueFrame"/>.
    /// </summary>
    internal interface IValueEntry
    {
        /// <summary>
        /// Gets the property that this value applies to.
        /// </summary>
        AvaloniaProperty Property { get; }

        /// <summary>
        /// Checks whether the entry has a value, starting the entry if necessary.
        /// </summary>
        bool HasValue();

        /// <summary>
        /// Gets the value associated with the entry.
        /// </summary>
        /// <exception cref="AvaloniaInternalException">
        /// The entry has no value.
        /// </exception>
        object? GetValue();

        /// <summary>
        /// Gets the data validation state if supported.
        /// </summary>
        /// <param name="state">The binding validation state.</param>
        /// <param name="error">The current binding error, if any.</param>
        /// <returns>
        /// True if the entry supports data validation, otherwise false.
        /// </returns>
        bool GetDataValidationState(out BindingValueType state, out Exception? error);

        /// <summary>
        /// Called when the value entry is removed from the value store.
        /// </summary>
        void Unsubscribe();

        /// <summary>
        /// Reads the value of an entry as <typeparamref name="T"/>, preferring the unboxed
        /// <see cref="IValueEntry{T}"/> path when available.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="value">The value, if the entry has one.</param>
        /// <returns>True if the entry has a value, otherwise false.</returns>
        static bool TryGetValue<T>(IValueEntry entry, out T value)
        {
            if (entry.HasValue())
            {
                value = entry is IValueEntry<T> typed ? typed.GetValue() : (T)entry.GetValue()!;
                return true;
            }

            value = default!;
            return false;
        }
    }
}
