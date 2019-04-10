using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls.Notifications
{
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
