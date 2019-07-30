// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// An owner of a <see cref="PriorityValue"/>.
    /// </summary>
    internal interface IPriorityValueOwner
    {
        /// <summary>
        /// Called when a <see cref="PriorityValue"/>'s value changes.
        /// </summary>
        /// <param name="property">The the property that has changed.</param>
        /// <param name="priority">The priority of the value.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        void Changed(AvaloniaProperty property, int priority, object oldValue, object newValue);

        /// <summary>
        /// Called when a <see cref="BindingNotification"/> is received by a 
        /// <see cref="PriorityValue"/>.
        /// </summary>
        /// <param name="property">The the property that has changed.</param>
        /// <param name="notification">The notification.</param>
        void BindingNotificationReceived(AvaloniaProperty property, BindingNotification notification);

        /// <summary>
        /// Returns deferred setter for given non-direct property.
        /// </summary>
        /// <param name="property">Property.</param>
        /// <returns>Deferred setter for given property.</returns>
        DeferredSetter<object> GetNonDirectDeferredSetter(AvaloniaProperty property);

        /// <summary>
        /// Logs a binding error.
        /// </summary>
        /// <param name="property">The property the error occurred on.</param>
        /// <param name="e">The binding error.</param>
        void LogError(AvaloniaProperty property, Exception e);

        /// <summary>
        /// Ensures that the current thread is the UI thread.
        /// </summary>
        void VerifyAccess();
    }
}
