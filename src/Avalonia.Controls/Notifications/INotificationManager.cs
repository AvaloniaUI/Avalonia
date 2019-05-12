// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// Represents a notification manager that can be used to show notifications in a window or using
	/// the host operating system.
    /// </summary>
    public interface INotificationManager
    {
        /// <summary>
        /// Show a notification.
        /// </summary>
        /// <param name="notification">The notification to be displayed.</param>
        void Show(INotification notification);
    }
}
