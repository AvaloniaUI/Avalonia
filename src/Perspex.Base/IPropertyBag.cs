// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex
{
    /// <summary>
    /// Interface for getting/setting <see cref="PerspexProperty"/> values on an object.
    /// </summary>
    public interface IPropertyBag
    {
        /// <summary>
        /// Clears a <see cref="PerspexProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        void ClearValue(PerspexProperty property);

        /// <summary>
        /// Gets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        object GetValue(PerspexProperty property);

        /// <summary>
        /// Gets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        T GetValue<T>(PerspexProperty<T> property);

        /// <summary>
        /// Checks whether a <see cref="PerspexProperty"/> is registered on this object.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        bool IsRegistered(PerspexProperty property);

        /// <summary>
        /// Checks whether a <see cref="PerspexProperty"/> is set on this object.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is set, otherwise false.</returns>
        bool IsSet(PerspexProperty property);

        /// <summary>
        /// Sets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        void SetValue(PerspexProperty property, object value, BindingPriority priority = BindingPriority.LocalValue);

        /// <summary>
        /// Sets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        void SetValue<T>(PerspexProperty<T> property, T value, BindingPriority priority = BindingPriority.LocalValue);
    }
}