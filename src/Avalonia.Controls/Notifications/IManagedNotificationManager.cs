// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// Interface for a managed notification manager.
    /// Managed notification managers can show any content.
    /// </summary>
    public interface IManagedNotificationManager : INotificationManager
    {
        /// <summary>
        /// Shows a notification.
        /// </summary>
        /// <param name="content">The content to be displayed.</param>
        void Show(object content);
    }
}
