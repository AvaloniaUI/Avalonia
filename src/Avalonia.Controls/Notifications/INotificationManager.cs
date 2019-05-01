using System;

namespace Avalonia.Controls.Notifications
{
    public interface INotificationManager
    {
        void Show(object content, TimeSpan? expirationTime = null, Action onClick = null, Action onClose = null);

        void Show(NotificationContent content, TimeSpan? expirationTime = null, Action onClick = null, Action onClose = null);
    }
}
