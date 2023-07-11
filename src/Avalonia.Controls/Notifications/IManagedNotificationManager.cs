using System;
using Avalonia.Metadata;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// Represents a notification manager that can show arbitrary content.
    /// Managed notification managers can show any content.
    /// </summary>
    /// <remarks>
    /// Because notification managers of this type are implemented purely in managed code, they
    /// can display arbitrary content, as opposed to notification managers which display notifications
    /// using the host operating system's notification mechanism.
    /// </remarks>
    [NotClientImplementable]
    public interface IManagedNotificationManager : INotificationManager
    {
        /// <summary>
        /// Shows a notification.
        /// </summary>
        /// <param name="content">The content to be displayed.</param>
        /// <param name="type">The <see cref="NotificationType"/> of the notification.</param>
        /// <param name="expiration">the expiration time of the notification after which it will automatically close. If the value is <see cref="TimeSpan.Zero"/> then the notification will remain open until the user closes it.</param>
        /// <param name="onClick">an Action to be run when the notification is clicked.</param>
        /// <param name="onClose">an Action to be run when the notification is closed.</param>
        void Show(object content,
            NotificationType type = NotificationType.Information,
            TimeSpan? expiration = null,
            Action? onClick = null,
            Action? onClose = null);
    }
}
