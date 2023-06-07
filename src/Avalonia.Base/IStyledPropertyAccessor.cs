using System;

namespace Avalonia
{
    /// <summary>
    /// Provides a runtime interface for interfacing with <see cref="StyledProperty{TValue}"/>.
    /// </summary>
    internal interface IStyledPropertyAccessor
    {
        /// <summary>
        /// Gets the default value for the property for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The default value.
        /// </returns>
        object? GetDefaultValue(Type type);

        /// <summary>
        /// Validates the specified property value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>True if the value is valid, otherwise false.</returns>
        bool ValidateValue(object? value);
    }
}
