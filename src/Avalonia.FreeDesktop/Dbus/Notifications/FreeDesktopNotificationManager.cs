using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Notifications;
using Avalonia.Notifications.Native;
using Tmds.DBus;

namespace Avalonia.FreeDesktop.Dbus.Notifications
{
    public class FreeDesktopNotificationManager : INativeNotificationManager, IDisposable
    {
        /// <summary>
        /// The notification's expiration time is dependent on the notification server's settings,
        /// and may vary for the type of notification.
        /// </summary>
        public const int DEFAULT_NOTIFICATION_EXPIRATION = -1;

        /// <summary>
        /// Never expire
        /// </summary>
        public const int FOREVER_NOTIFICATION_EXPIRATION = 0;

        private readonly IFreeDesktopNotificationsProxy _proxy;
        private IDisposable _actionWatcher;
        private IDisposable _closeNotificationWatcher;
        private volatile bool _isConnected;

        private ConcurrentDictionary<uint, INotification> _notificationsList
            = new ConcurrentDictionary<uint, INotification>();

        public FreeDesktopNotificationManager()
        {
            IsAvailable()
                .ContinueWith(t => _isConnected = t.Result, TaskContinuationOptions.OnlyOnRanToCompletion);

            _proxy = Connection.Session.CreateProxy<IFreeDesktopNotificationsProxy>(
                FreeDesktopDbusInfo.NotificationsService,
                FreeDesktopDbusInfo.NotificationsPath
            );

            var actionWatcherTask = _proxy.WatchActionInvokedAsync(
                OnNotificationActionInvoked,
                OnNotificationActionInvokedError
            );
            var closeNotificationWatcherTask = _proxy.WatchNotificationClosedAsync(
                OnNotificationClosed,
                OnNotificationClosedError
            );

            HandleWatcherTasks(actionWatcherTask, closeNotificationWatcherTask);
        }

        public void Show(INotification notification)
        {
            throw new NotSupportedException("Use the async version");
        }

        public Task ShowAsync(INotification notification)
        {
            return ShowInternalAsync(notification);
        }

        public Task ReplaceAsync(INotification old, INotification @new)
        {
            if (!old.Id.HasValue)
                throw new ArgumentException("Cannot replace a notification that was not shown.", nameof(old));

            return ShowInternalAsync(@new, old.Id.Value);
        }


        public void Close(INotification notification)
        {
            throw new NotSupportedException("Use the async version");
        }

        public async Task CloseAsync(INotification notification)
        {
            if (!notification.Id.HasValue)
                throw new ArgumentException("This notification does not have an id.", nameof(notification));

            await _proxy.CloseNotificationAsync(notification.Id.Value)
                .ConfigureAwait(false);
        }

        public Task<string[]> GetCapabilitiesAsync()
        {
            return _proxy.GetCapabilitiesAsync();
        }

        public async Task<ServerInfo> GetServerInfoAsync()
        {
            var (name, vendor, version, specVersion) = await _proxy.GetServerInformationAsync()
                .ConfigureAwait(false);

            return new ServerInfo(name, vendor, version, specVersion);
        }

        public Task<bool> IsAvailable()
        {
            return Connection.Session.IsServiceActiveAsync(FreeDesktopDbusInfo.NotificationsService);
        }

        public void Dispose()
        {
            _actionWatcher?.Dispose();
            _closeNotificationWatcher?.Dispose();
        }

        private async Task ShowInternalAsync(INotification notification, uint replaceId = 0)
        {
            if (!_isConnected)
            {
                await ShowNotConnectedErrorOnConsole();
                return;
            }

            if (notification.Id.HasValue)
                throw new ArgumentException("This notification was previously used.", nameof(notification));

            var id = await _proxy.NotifyAsync(
                Application.Current.Name,
                replaceId,
                //TODO: Impl app icon
                string.Empty,
                notification.Title,
                notification.Message,
                GetActionsFromNotification(notification),
                GetHintsFromNotification(notification),
                GetExpirationInMsFromNotification(notification)
            ).ConfigureAwait(false);

            _notificationsList[id] = notification;
        }

        private static string[] GetActionsFromNotification(INotification notification)
        {
            if (notification is NativeNotification nn && nn.Actions != null)
            {
                var actionPair = new List<string> { "default", "default" };

                foreach (var action in nn.Actions)
                {
                    actionPair.Add(action.Key);
                    actionPair.Add(action.Label);
                }

                return actionPair.ToArray();
            }

            return new[] { "default", "default" };
        }

        private static int GetExpirationInMsFromNotification(INotification notification)
        {
            if (notification.Expiration == TimeSpan.MaxValue)
                return FOREVER_NOTIFICATION_EXPIRATION;
            if (notification.Expiration == TimeSpan.MinValue)
                return DEFAULT_NOTIFICATION_EXPIRATION;

            var expirationInMs = notification.Expiration == TimeSpan.Zero ?
                DEFAULT_NOTIFICATION_EXPIRATION :
                (int)notification.Expiration.TotalMilliseconds;

            return expirationInMs;
        }

        private Dictionary<string, object> GetHintsFromNotification(INotification notification)
        {
            if (notification is NativeNotification nn)
            {
                var hints = new Dictionary<string, object> { { "urgency", (byte)nn.Urgency } };

                //TODO: The others https://developer.gnome.org/notification-spec/#id2825136
                if (nn.Resident.HasValue)
                    hints.Add("resident", (byte)(nn.Resident.Value ? 1 : 0));

                if (nn.Transient.HasValue)
                    hints.Add("transient", (byte)(nn.Transient.Value ? 1 : 0));

                return hints;
            }

            var urgency = GetUrgencyFromType(notification.Type);
            return new Dictionary<string, object>(1) { { "urgency", urgency } };
        }

        private static byte GetUrgencyFromType(NotificationType type)
        {
            //TODO: Move this method somewhere else
            byte urgency;

            switch (type)
            {
                case NotificationType.Warning:
                    urgency = 1;
                    break;
                case NotificationType.Error:
                    urgency = 2;
                    break;
                default:
                    urgency = 0;
                    break;
            }

            return urgency;
        }

        private void OnNotificationActionInvokedError(Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
        }

        private void OnNotificationActionInvoked((uint id, string actionKey) e)
        {
            Debug.WriteLine("Action invoked signal: {0} {1}", e.id.ToString(), e.actionKey);

            try
            {
                INotification notification;
                // ReSharper disable once EmptyEmbeddedStatement
                while (!_notificationsList.TryGetValue(e.id, out notification)) ;

                if (e.actionKey == "default")
                    notification.OnClick?.Invoke();

                if (notification is NativeNotification nn)
                {
                    nn.OnActionInvoked(new ActionInvokedEventArgs(e.actionKey));
                }
            }
            catch (Exception exception)
            {
                //TODO: Impl better exception handling
                Console.WriteLine(exception);
            }
        }

        private void OnNotificationClosed((uint id, uint reason) e)
        {
            var reason = (NativeNotificationCloseReason)e.reason;

            Debug.WriteLine("Notification closed signal: {0} {1}:{2}", e.id.ToString(), e.reason.ToString(), reason);

            if (!_notificationsList.ContainsKey(e.id))
                return;

            try
            {
                INotification notification;
                // ReSharper disable once EmptyEmbeddedStatement
                while (!_notificationsList.TryRemove(e.id, out notification)) ;

                if (notification is NativeNotification nn)
                    nn.OnClose?.Invoke(reason);
                else
                    notification.OnClose?.Invoke();
            }
            catch (Exception exception)
            {
                //TODO: Impl better exception handling
                Console.WriteLine(exception);
            }
        }

        private void OnNotificationClosedError(Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
        }

        private void HandleWatcherTasks(
            Task<IDisposable> actionWatcherTask,
            Task<IDisposable> closeNotificationWatcherTask
        )
        {
            IDisposable HandleContinuedTask(Task<IDisposable> t)
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    var result = t.Result;
                    t.Dispose();
                    return result;
                }

                //TODO: the rest
                return null;
            }

            actionWatcherTask
                .ContinueWith(
                    t => _actionWatcher = HandleContinuedTask(t),
                    TaskContinuationOptions.OnlyOnRanToCompletion
                );
            closeNotificationWatcherTask
                .ContinueWith(
                    t => _closeNotificationWatcher = HandleContinuedTask(t),
                    TaskContinuationOptions.OnlyOnRanToCompletion
                );
        }

        private static ConfiguredTaskAwaitable ShowNotConnectedErrorOnConsole()
        {
            return Console.Error
                .WriteLineAsync("The notification manager is not connected to the current DBus session.")
                .ConfigureAwait(false);
        }
    }
}
