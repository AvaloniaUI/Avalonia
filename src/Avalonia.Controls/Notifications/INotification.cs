using System;
using Avalonia.Metadata;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// Represents a notification that can be shown in a window or by the host operating system.
    /// </summary>
    [NotClientImplementable]
    public interface INotification
    {
        /// <summary>
        /// Gets the Title of the notification.
        /// </summary>
        string? Title { get; }

        /// <summary>
        /// Gets the notification message.
        /// </summary>
        string? Message { get; }

        /// <summary>
        /// Gets the <see cref="NotificationType"/> of the notification.
        /// </summary>
        NotificationType Type { get; }

        /// <summary>
        /// Gets the expiration time of the notification after which it will automatically close.
        /// If the value is <see cref="TimeSpan.Zero"/> then the notification will remain open until the user closes it.
        /// </summary>
        TimeSpan Expiration { get; }

        /// <summary>
        /// Gets an Action to be run when the notification is clicked.
        /// </summary>
        Action? OnClick { get; }

        /// <summary>
        /// Gets an Action to be run when the notification is closed.
        /// </summary>
        Action? OnClose { get; }
    }
}
