using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Avalonia.Controls.Notifications;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.Notifications
{
    public class Win10NotificationManager : INotificationManager
    {
        private readonly string _appUserModelId;
        private readonly Dictionary<ToastNotification, INotification> _associatedNotifications;

        public Win10NotificationManager()
        {
            //TODO: When to install and configure the appUserModelId and shortcut?
            const string appName = "TEST";

            _associatedNotifications = new Dictionary<ToastNotification, INotification>();
            _appUserModelId = $"{appName}.App";

            UnmanagedMethods.SetCurrentProcessExplicitAppUserModelID(_appUserModelId);
            InstallStartMenuShortcut(appName, _appUserModelId);
        }

        private static void InstallStartMenuShortcut(string shortcutName, string appUserModelId)
        {
            var path = Assembly.GetExecutingAssembly().Location;
            using var shortcut = new ShellLink
            {
                TargetPath = path,
                Arguments = string.Empty,
                AppUserModelID = appUserModelId
            };

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var startMenuPath = Path.Combine(appData, @"Microsoft\Windows\Start Menu\Programs");
            var shortcutFile = Path.Combine(startMenuPath, $"{shortcutName}.lnk");

            shortcut.Save(shortcutFile);
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

            var toastNotifier = ToastNotificationManager.CreateToastNotifier(_appUserModelId);
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
