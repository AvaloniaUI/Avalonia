// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        object GetDefaultValue(Type type);

        /// <summary>
        /// Gets a validation function for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The validation function, or null if no validation function exists.
        /// </returns>
        Func<IAvaloniaObject, object, object> GetValidationFunc(Type type);
    }
}
