// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Data;

namespace Perspex.Markup.Data.Plugins
{
    /// <summary>
    /// Defines an accessor to a property on an object returned by a 
    /// <see cref="IPropertyAccessorPlugin"/>
    /// </summary>
    public interface IPropertyAccessor : IDisposable
    {
        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        Type PropertyType { get; }

        /// <summary>
        /// Gets the current value of the property.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="value">
        /// The value to set. Guaranteed to be of a valid type for the property.
        /// </param>
        /// <param name="priority">
        /// The priority with which to set the value.
        /// </param>
        /// <returns>
        /// True if the property was set; false if the property could not be set.
        /// </returns>
        bool SetValue(object value, BindingPriority priority);
    }
}
