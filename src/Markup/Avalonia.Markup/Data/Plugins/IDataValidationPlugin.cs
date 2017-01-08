// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Defines how data validation is observed by an <see cref="ExpressionObserver"/>.
    /// </summary>
    public interface IDataValidationPlugin
    {
        /// <summary>
        /// Checks whether this plugin can handle data validation on the specified object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <param name="memberName">The name of the member to validate.</param>
        /// <returns>True if the plugin can handle the object; otherwise false.</returns>
        bool Match(WeakReference reference, string memberName);

        /// <summary>
        /// Starts monitoring the data validation state of a property on an object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="inner">The inner property accessor used to aceess the property.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        IPropertyAccessor Start(
            WeakReference reference,
            string propertyName,
            IPropertyAccessor inner);
    }
}
