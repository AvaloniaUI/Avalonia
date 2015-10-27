// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Markup.Data.Plugins
{
    /// <summary>
    /// Defines how a member is read, written and observed by a 
    /// <see cref="ExpressionObserver"/>.
    /// </summary>
    public interface IPropertyAccessorPlugin
    {
        /// <summary>
        /// Checks whether this plugin can handle accessing the properties of the specified object.
        /// </summary>
        /// <param name="instance">The object.</param>
        /// <returns>True if the plugin can handle the object; otherwise false.</returns>
        bool Match(object instance);

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="instance">The object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="changed">A function to call when the property changes.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made, or null if the property was not found.
        /// </returns>
        IPropertyAccessor Start(object instance, string propertyName, Action<object> changed);
    }
}
