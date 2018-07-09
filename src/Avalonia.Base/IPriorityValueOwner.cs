// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;

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
        /// Ensures that the current thread is the UI thread.
        /// </summary>
        void VerifyAccess();
    }
}
