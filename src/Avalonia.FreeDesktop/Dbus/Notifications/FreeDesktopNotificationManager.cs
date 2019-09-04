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

        public async Task ShowAsync(INotification notification)
        {
            if (!_isConnected)
            {
                await ShowNotConnectedErrorOnConsole();
                return;
            }

            if (notification.Id != default)
                throw new ArgumentException("This was previously used.", nameof(notification));

            var expirationInMs = notification.Expiration == TimeSpan.Zero ?
                DEFAULT_NOTIFICATION_EXPIRATION :
                (int)notification.Expiration.TotalMilliseconds;

            var id = await _proxy.NotifyAsync(
                Application.Current.Name,
                //TODO: Maybe a control to always replace
                0,
                //TODO: Impl app icon
                string.Empty,
                notification.Title,
                notification.Message,
                GetActionsFromNotification(notification),
                GetHintsFromNotification(notification),
                expirationInMs
            ).ConfigureAwait(false);

            _notificationsList[id] = notification;
        }

        private static string[] GetActionsFromNotification(INotification notification)
        {
            if (notification is NativeNotification nn)
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

        public void Close(INotification notification)
        {
            throw new NotSupportedException("Use the async version");
        }

        public async Task CloseAsync(INotification notification)
        {
            if (notification.Id == default)
                throw new ArgumentException("This notification does not have an id.", nameof(notification));

            await _proxy.CloseNotificationAsync(notification.Id)
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

        private Dictionary<string, object> GetHintsFromNotification(INotification notification)
        {
            byte urgency;

            //TODO: Change this into an enum
            switch (notification.Type)
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

            var hints = new Dictionary<string, object> { { "urgency", urgency } };

            if (notification is NativeNotification nativeNotification)
            {
                //TODO: The others https://developer.gnome.org/notification-spec/#id2825136
                if (nativeNotification.Resident.HasValue)
                    hints.Add("resident", (byte)(nativeNotification.Resident.Value ? 1 : 0));

                if (nativeNotification.Transient.HasValue)
                    hints.Add("transient", (byte)(nativeNotification.Transient.Value ? 1 : 0));
            }

            return hints;
        }

        private void OnNotificationActionInvokedError(Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
        }

        private void OnNotificationActionInvoked((uint id, string actionKey) e)
        {
            Debug.WriteLine("Action invoked signal: {0} {1}", e.id.ToString(), e.actionKey);
            {
                INotification notification;
                while (!_notificationsList.TryGetValue(e.id, out notification)) ;

                try
                {
                    if (e.actionKey == "default")
                        notification.OnClick?.Invoke();
                    else if (notification is NativeNotification nn)
                    {
                        nn.OnActionInvoked(new ActionInvokedHandlerArgs(e.actionKey));
                    }
                }
                catch (Exception exception)
                {
                    //TODO: Impl better exception handling
                    Console.WriteLine(exception);
                }
            }
        }

        private void OnNotificationClosed((uint id, uint reason) e)
        {
            string reason;
            switch (e.reason)
            {
                case 1:
                    reason = "The notification expired";
                    break;
                case 2:
                    reason = "The notification was dismissed by the user";
                    break;
                case 3:
                    reason = "The notification was closed by a call to CloseNotification";
                    break;
                case 4:
                    reason = "Undefined/reserved reasons";
                    break;
                default:
                    reason = "Unknown reason";
                    break;
            }

            Console.WriteLine("Notification closed signal: {0} {1}:{2}", e.id.ToString(), e.reason.ToString(), reason);
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
