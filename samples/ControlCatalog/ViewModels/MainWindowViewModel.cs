using Avalonia;
using Avalonia.Controls.Notifications;
using JetBrains.Annotations;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly INotificationManager _nativeNotificationManager;
        private IManagedNotificationManager _notificationManager;

        public MainWindowViewModel(IManagedNotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
            _nativeNotificationManager = AvaloniaLocator.Current.GetService<INotificationManager>();
        }

        public IManagedNotificationManager NotificationManager
        {
            get { return _notificationManager; }
            set { this.RaiseAndSetIfChanged(ref _notificationManager, value); }
        }

        [UsedImplicitly]
        public void ShowManagedNotification()
        {
            NotificationManager.Show(new Notification(
                "Welcome",
                "Avalonia now supports Notifications."));
        }

        [UsedImplicitly]
        public void ShowNativeNotification()
        {
            if (_nativeNotificationManager != null)
            {
                _nativeNotificationManager.Show(new Notification(
                    "Native",
                    "Native Notifications are finally here!",
                    NotificationType.Success));
            }
            else
            {
                NotificationManager.Show(new Notification(
                    "Native",
                    "Native Notifications are not supported on this platform!",
                    NotificationType.Error));
            }
        }

        [UsedImplicitly]
        public void ShowCustomManagedNotification()
        {
            NotificationManager.Show(
                new NotificationViewModel(NotificationManager)
                {
                    Title = "Hey There!",
                    Message = "Did you know that Avalonia now supports Custom In-Window Notifications?"
                });
        }
    }
}
