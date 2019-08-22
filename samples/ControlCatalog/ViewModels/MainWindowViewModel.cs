using System.Reactive;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Notifications.Native;
using ReactiveUI;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel : ReactiveObject
    {
        public MainWindowViewModel(
            IManagedNotificationManager notificationManager,
            INativeNotificationManager nativeNotificationManager
        )
        {
            ShowCustomManagedNotificationCommand = ReactiveCommand.Create(() =>
            {
                notificationManager.Show(new NotificationViewModel(notificationManager) { Title = "Hey There!", Message = "Did you know that Avalonia now supports Custom In-Window Notifications?" });
            });

            ShowManagedNotificationCommand = ReactiveCommand.Create(() =>
            {
                notificationManager.Show(new Notification("Welcome", "Avalonia now supports Notifications.", NotificationType.Information));
            });

            ShowNativeNotificationCommand = ReactiveCommand.Create(() =>
            {
                //TODO: Change
                nativeNotificationManager.Show(
                    new Notification(
                        "Native Notifications",
                        "Fluid and natural native notifications",
                        NotificationType.Error
                    )
                );
            });
        }

        public ReactiveCommand<Unit, Unit> ShowCustomManagedNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowManagedNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowNativeNotificationCommand { get; }
    }
}
