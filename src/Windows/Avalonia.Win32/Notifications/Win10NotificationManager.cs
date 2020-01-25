using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reflection;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Avalonia.Controls.Notifications;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.Notifications
{
    internal class Win10NotificationManager : INotificationManager
    {
        private readonly Dictionary<ToastNotification, INotification> _associatedNotifications;

        public Win10NotificationManager()
        {
            _associatedNotifications = new Dictionary<ToastNotification, INotification>();
        }

        public void Show(INotification notification)
        {
            var toastXml =
                $@"<toast><visual>
            <binding template='ToastGeneric'>
            <text>{notification.Title}</text>
            <text>{notification.Message}</text>
            </binding>
        </visual></toast>";

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(toastXml);

            var toastNotification = new ToastNotification(xmlDoc)
            {
                ExpirationTime = DateTimeOffset.Now + notification.Expiration
            };

            toastNotification.Activated += ToastNotificationOnActivated;
            toastNotification.Dismissed += ToastNotificationOnDismissed;
            toastNotification.Failed += ToastNotificationOnFailed;

            var toastNotifier = ToastNotificationManager.CreateToastNotifier(Win32Platform.AppUserModelId);
            toastNotifier.Show(toastNotification);

            _associatedNotifications[toastNotification] = notification;
        }

        private static void ToastNotificationOnFailed(ToastNotification sender, ToastFailedEventArgs args)
        {
            Console.Error.WriteLine(args.ErrorCode);
        }

        private void ToastNotificationOnDismissed(ToastNotification sender, ToastDismissedEventArgs args)
        {
            if (_associatedNotifications.TryGetValue(sender, out var notification))
            {
                notification.OnClose?.Invoke();
            }
        }

        private void ToastNotificationOnActivated(ToastNotification sender, object args)
        {
            if (_associatedNotifications.TryGetValue(sender, out var notification))
            {
                notification.OnClick?.Invoke();
            }
        }
    }
}
