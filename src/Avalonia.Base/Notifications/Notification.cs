// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Avalonia.Notifications
{
    /// <summary>
    /// A notification that can be shown in a window or by the host operating system.
    /// </summary>
    /// <remarks>
    /// This class represents a notification that can be displayed either in a window using
    /// <see cref="INotificationManager"/> and its implementations.
    /// </remarks>
    public class Notification : INotification
    {
        protected INotificationManager NotificationManager;

        /// <summary>
        /// Used if no expiration time is provided when creating a new instance of <see cref="Notification"/>
        /// </summary>
        public static TimeSpan DefaultExpiration = TimeSpan.FromSeconds(5);

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
            Action onClose = null
        )
        {
            Title = title;
            Message = message;
            Type = type;
            Expiration = expiration.HasValue ? expiration.Value : DefaultExpiration;
            OnClick = onClick;
            OnClose = onClose;
        }

        /// <inheritdoc/>
        public virtual uint? Id { get; private set; }

        /// <inheritdoc/>
        public virtual string Title { get; private set; }

        /// <inheritdoc/>
        public virtual string Message { get; private set; }

        /// <inheritdoc/>
        public virtual NotificationType Type { get; private set; }

        /// <inheritdoc/>
        public virtual TimeSpan Expiration { get; private set; }

        /// <inheritdoc/>
        public virtual Action OnClick { get; private set; }

        /// <inheritdoc/>
        public virtual Action OnClose { get; private set; }

        /// <inheritdoc/>
        public virtual Task CloseAsync()
        {
            NotificationManager.Close(this);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void Close()
        {
            NotificationManager.Close(this);
        }

        /// <inheritdoc/>
        public virtual INotification Clone()
        {
            return new Notification(
                Title,
                Message,
                Type,
                Expiration,
                OnClick,
                OnClose
            );
        }

        /// <inheritdoc/>
        public virtual void SetId(uint id, [NotNull] INotificationManager notificationManager)
        {
            if (id == 0)
                throw new ArgumentOutOfRangeException(nameof(id));

            Id = id;
            NotificationManager = notificationManager ?? throw new ArgumentNullException(nameof(notificationManager));
        }
    }
}
