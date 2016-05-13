// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Defines how view model data validation is observed by an <see cref="ExpressionObserver"/>.
    /// </summary>
    public interface IValidationPlugin
    {

        /// <summary>
        /// Checks whether the data uses a validation scheme supported by this plugin.
        /// </summary>
        /// <param name="reference">A weak reference to the data.</param>
        /// <returns><c>true</c> if this plugin can observe the validation; otherwise, <c>false</c>.</returns>
        bool Match(WeakReference reference);

        /// <summary>
        /// Starts monitoring the validation state of an object for the given property.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <param name="name">The property name.</param>
        /// <param name="accessor">An underlying <see cref="IPropertyAccessor"/> to access the property.</param>
        /// <param name="callback">A function to call when the validation state changes.</param>
        /// <returns>
        /// A <see cref="ValidatingPropertyAccessorBase"/> subclass through which future interactions with the 
        /// property will be made.
        /// </returns>
        IPropertyAccessor Start(WeakReference reference, string name, IPropertyAccessor accessor, Action<IValidationStatus> callback);
    }
}
