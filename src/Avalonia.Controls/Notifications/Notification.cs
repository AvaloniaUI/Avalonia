// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// A notification that can be shown in a window or by the host operating system.
    /// </summary>
    /// <remarks>
    /// This class represents a notification that can be displayed either in a window using
    /// <see cref="WindowNotificationManager"/> or by the host operating system (to be implemented).
    /// </remarks>
    public class Notification : INotification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        /// <param name="title">The title of the notification.</param>
        /// <param name="message">The message to be displayed in the notification.</param>
        /// <param name="type">The <see cref="NotificationType"/> of the notification.</param>
        /// <param name="expiration">The expiry time at which the notification will close. 
        /// Use <see cref="TimeSpan.Zero"/> for notifications that will remain open.</param>
        /// <param name="onClick">An Action to call when the notification is clicked.</param>
        /// <param name="onClose">An Action to call when the notification is closed.</param>
        public Notification(string title,
            string message,
            NotificationType type = NotificationType.Information,
            TimeSpan? expiration = null,
            Action onClick = null,
            Action onClose = null)
        {
            Title = title;
            Message = message;
            Type = type;
            Expiration = expiration.HasValue ? expiration.Value : TimeSpan.FromSeconds(5);
            OnClick = onClick;
            OnClose = onClose;
        }

        /// <inheritdoc/>
        public string Title { get; private set; }

        /// <inheritdoc/>
        public string Message { get; private set; }

        /// <inheritdoc/>
        public NotificationType Type { get; private set; }

        /// <inheritdoc/>
        public TimeSpan Expiration { get; private set; }

        /// <inheritdoc/>
        public Action OnClick { get; private set; }

        /// <inheritdoc/>
        public Action OnClose { get; private set; }
    }
}
