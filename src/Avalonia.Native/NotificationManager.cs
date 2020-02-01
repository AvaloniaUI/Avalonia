using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    public class AvaloniaNativeNotificationManager : INotificationManager
    {
        private delegate void NotificationCloseDelegate(int id);
        private delegate void NotificationActionDelegate(int id);
    
        private readonly IAvnNotificationManager _native;
        private readonly Dictionary<int, INotification> _notifications;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly NotificationCloseDelegate _notificationCloseCallback;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly NotificationActionDelegate _notificationActionCallback;
        private readonly Random _idGenerator;
 
        public AvaloniaNativeNotificationManager(IAvnNotificationManager native)
        {
            _native = native;
            _notifications = new Dictionary<int, INotification>();
            _notificationCloseCallback = NotificationOnClose;
            _notificationActionCallback = NotificationOnAction;
            _native.CloseCallback = Marshal.GetFunctionPointerForDelegate(_notificationCloseCallback);
            _native.ActionCallback = Marshal.GetFunctionPointerForDelegate(_notificationActionCallback);
            _idGenerator = new Random();
        }

        private void NotificationOnClose(int id)
        {
            if (_notifications.TryGetValue(id, out var notification))
            {
                notification.OnClose?.Invoke();
                _notifications.Remove(id);
            }
        }
        
        private void NotificationOnAction(int id)
        {
            if (_notifications.TryGetValue(id, out var notification))
            {
                notification.OnClick?.Invoke();
            }
        }
    
#pragma warning disable 1998
        public async ValueTask ShowAsync(INotification notification)
#pragma warning restore 1998
        {
            var nativeNotification = new AvnNotification
            { 
                Identifier = _idGenerator.Next(),
                TitleUtf8 = notification.Title,
                TextUtf8 = notification.Message,
                DurationMs = (int) notification.Expiration.TotalMilliseconds
            };

            _notifications[nativeNotification.Identifier] = notification;
            _native.ShowNotification(ref nativeNotification);
        }
    }
}
