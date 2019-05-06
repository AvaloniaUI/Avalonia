using System;

namespace Avalonia.Controls.Notifications
{
    public interface INotification
    {
        string Title { get; }

        string Message { get; }

        NotificationType Type { get; }

        TimeSpan Expiration { get; }

        Action OnClick { get; }

        Action OnClose { get; }
    }

    public interface INotificationManager
    {
        void Show(INotification notification);
    }

    public interface IManagedNotificationManager : INotificationManager
    {
        void Show(object content);
    }
}
