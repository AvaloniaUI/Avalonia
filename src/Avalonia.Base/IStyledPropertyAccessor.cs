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
    }
}
