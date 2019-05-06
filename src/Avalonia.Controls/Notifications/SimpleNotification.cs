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
    public class SimpleNotification : INotification
    {
        public static INotification Create (
            string title, 
            string message, 
            NotificationType type = NotificationType.Information, 
            TimeSpan? expiration = null, 
            Action onClick = null, 
            Action onClose = null)
        {
            var result = new SimpleNotification
            {
                Title = title,
                Message = message,
                Type = type,
                Expiration = expiration.HasValue ? expiration.Value : TimeSpan.FromSeconds(5),
                OnClick = onClick,
                OnClose = onClose
            };

            return result;
        }

        public string Title { get; private set; }

        public string Message { get; private set; }

        public NotificationType Type { get; private set; }

        public TimeSpan Expiration { get; private set; }

        public Action OnClick { get; private set; }

        public Action OnClose { get; private set; }
    }

    public enum NotificationType
    {
        Information,
        Success,
        Warning,
        Error
    }
}
