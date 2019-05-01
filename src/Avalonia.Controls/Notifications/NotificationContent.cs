using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// Defines content for a <see cref="Notification"/> control.
    /// </summary>
    /// <remarks>
    /// This notification content type is compatible with native notifications.
    /// </remarks>
    public class NotificationContent
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
    }

    public enum NotificationType
    {
        Information,
        Success,
        Warning,
        Error
    }
}
