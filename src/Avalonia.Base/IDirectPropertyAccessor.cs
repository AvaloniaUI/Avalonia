// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia
{
    /// <summary>
    /// Provides a runtime interface for getting and setting 
    /// <see cref="DirectProperty{TOwner, TValue}"/> values.
    /// </summary>
    internal interface IDirectPropertyAccessor
    {
        /// <summary>
        /// Gets a value indicating whether the property is read-only.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets the value of the property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>The property value.</returns>
        object GetValue(IAvaloniaObject instance);

        /// <summary>
        /// Sets the value of the property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="value">The value.</param>
        void SetValue(IAvaloniaObject instance, object value);
    }
}
