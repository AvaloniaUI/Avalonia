using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Notifications.Native;
using Tmds.DBus;

namespace Avalonia.FreeDesktop.Dbus.Notifications
{
    public class FreeDesktopNotificationManager : INativeNotificationManager, IDisposable
    {
        internal const int FREE_DESKTOP_NOTIFICATION_DEFAULT_EXPIRATION = 0;
        internal const int FREE_DESKTOP_NOTIFICATION_DEFAULT_FOREVER = -1;

        private readonly IFreeDesktopNotificationsProxy _proxy;
        private IDisposable _actionWatcher;
        private IDisposable _closeNotificationWatcher;

        public FreeDesktopNotificationManager()
        {
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
            var expirationInMs = notification.Expiration == TimeSpan.Zero ?
                FREE_DESKTOP_NOTIFICATION_DEFAULT_EXPIRATION :
                notification.Expiration.Milliseconds;

            _proxy.NotifyAsync(
                Application.Current.Name,
                //TODO: Maybe a control to always replace
                0,
                //TODO: Impl app icon
                string.Empty,
                notification.Title,
                notification.Message,
                new string[0],
                GetHintsFromNotification(notification), 
                expirationInMs
            );
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
                case NotificationType.Information:
                case NotificationType.Success:
                default:
                    urgency = 0;
                    break;
            }
            
            return new Dictionary<string, object>
            {
                { "urgency",  urgency },
                //TODO: The others (http://www.galago-project.org/specs/notification/0.9/x344.html)
            };
        }

        private void OnNotificationActionInvokedError(Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
        }

        private void OnNotificationActionInvoked((uint id, string actionKey) e)
        {
            Console.WriteLine("Action invoked signal: {0} {1}", e.id.ToString(), e.actionKey);
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
                .ContinueWith(t => _actionWatcher = HandleContinuedTask(t));
            closeNotificationWatcherTask
                .ContinueWith(t => _closeNotificationWatcher = HandleContinuedTask(t));
        }
    }
}
