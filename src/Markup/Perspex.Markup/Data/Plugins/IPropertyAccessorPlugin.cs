// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;

namespace Perspex.Markup.Data.Plugins
{
    /// <summary>
    /// Defines how a member is read, written and observed by an
    /// <see cref="ExpressionObserver"/>.
    /// </summary>
    public interface IPropertyAccessorPlugin
    {
        /// <summary>
        /// Checks whether this plugin can handle accessing the properties of the specified object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <returns>True if the plugin can handle the object; otherwise false.</returns>
        bool Match(WeakReference reference);

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="changed">A function to call when the property changes.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        IPropertyAccessor Start(
            WeakReference reference, 
            string propertyName, 
            Action<object> changed);
    }
}
