using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Tmds.DBus;

namespace Avalonia.FreeDesktop.Notifications
{
    public class FreeDesktopNotificationManager : INotificationManager, IDisposable
    {
        private const string NotificationsService
            = "org.freedesktop.Notifications";

        // ReSharper disable once InconsistentNaming
        private const int DEFAULT_NOTIFICATION_EXPIRATION = -1;

        // ReSharper disable once InconsistentNaming
        private const int FOREVER_NOTIFICATION_EXPIRATION = 0;

        private static readonly ObjectPath NotificationsPath
            = new ObjectPath("/org/freedesktop/Notifications");

        private readonly Dictionary<uint, INotification> _notifications;

        private readonly IFreeDesktopNotificationsProxy _proxy;
        private IDisposable _actionWatcher;
        private IDisposable _closeNotificationWatcher;
        private volatile bool _isConnected;

        public FreeDesktopNotificationManager()
        {
            _notifications = new Dictionary<uint, INotification>();
            _proxy = Connection.Session.CreateProxy<IFreeDesktopNotificationsProxy>(
                NotificationsService,
                NotificationsPath
            );

            Connect();
            SetupWatcherTasks();
        }

        public void Dispose()
        {
            _actionWatcher?.Dispose();
            _closeNotificationWatcher?.Dispose();
        }

        public async void Show(INotification notification)
        {
            throw new NotSupportedException("Use the async version");
        }

        private async void Connect()
        {
            _isConnected = await
                Connection.Session.IsServiceActiveAsync(NotificationsService);
        }

        private async void SetupWatcherTasks()
        {
            _actionWatcher = await _proxy.WatchActionInvokedAsync(
                OnNotificationActionInvoked,
                OnNotificationActionInvokedError
            );
            _closeNotificationWatcher = await _proxy.WatchNotificationClosedAsync(
                OnNotificationClosed,
                OnNotificationClosedError
            );
        }

        private async Task ShowInternalAsync(INotification notification)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException();
            }

            var id = await _proxy.NotifyAsync(
                Application.Current.Name,
                0,
                //TODO: Impl app icon
                string.Empty,
                notification.Title,
                notification.Message,
                Array.Empty<string>(),
                GetHintsFromNotification(notification),
                GetExpirationInMsFromNotification(notification)
            ).ConfigureAwait(false);

            _notifications[id] = notification;
        }

        private static int GetExpirationInMsFromNotification(INotification notification)
        {
            if (notification.Expiration == TimeSpan.MaxValue)
            {
                return FOREVER_NOTIFICATION_EXPIRATION;
            }

            if (notification.Expiration == TimeSpan.MinValue ||
                notification.Expiration == TimeSpan.Zero)
            {
                return DEFAULT_NOTIFICATION_EXPIRATION;
            }

            return (int)notification.Expiration.TotalMilliseconds;
        }

        private static Dictionary<string, object> GetHintsFromNotification(INotification notification)
        {
            return new Dictionary<string, object>(1) { { "urgency", GetUrgencyFromType(notification.Type) } };
        }

        private static byte GetUrgencyFromType(NotificationType type)
        {
            //TODO: Move this method somewhere else

            return type switch
            {
                NotificationType.Warning => 1,
                NotificationType.Error => 2,
                _ => 0
            };
        }

        private static void OnNotificationActionInvokedError(Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
        }

        private void OnNotificationActionInvoked((uint id, string actionKey) args)
        {
            Debug.WriteLine($"Action invoked signal: {args.id} Action: {args.actionKey}");

            if (_notifications.TryGetValue(args.id, out var notification))
            {
                notification.OnClick?.Invoke();
            }
        }

        private void OnNotificationClosed((uint id, uint reason) args)
        {
            Debug.WriteLine($"Notification closed signal: {args.id} Reason: {args.reason}");

            if (_notifications.TryGetValue(args.id, out var notification))
            {
                notification.OnClick?.Invoke();
            }
        }

        private static void OnNotificationClosedError(Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
        }
    }
}
