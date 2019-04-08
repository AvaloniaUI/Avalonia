using System;

namespace Avalonia.Controls.Notifications
{
    interface INotificationManager
    {
        void Show(object content, string areaName = "", TimeSpan? expirationTime = null, Action onClick = null, Action onClose = null);
    }
}
