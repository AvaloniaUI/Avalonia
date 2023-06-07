using Avalonia.Controls.Notifications;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class NotificationViewModel
    {
        public WindowNotificationManager? NotificationManager { get; set; }

        public NotificationViewModel()
        {
            ShowCustomManagedNotificationCommand = MiniCommand.Create(() =>
            {
                NotificationManager?.Show(new NotificationViewModel() { Title = "Hey There!", Message = "Did you know that Avalonia now supports Custom In-Window Notifications?" , NotificationManager = NotificationManager});
            });

            ShowManagedNotificationCommand = MiniCommand.Create(() =>
            {
                NotificationManager?.Show(new Avalonia.Controls.Notifications.Notification("Welcome", "Avalonia now supports Notifications.", NotificationType.Information));
            });

            ShowNativeNotificationCommand = MiniCommand.Create(() =>
            {
                NotificationManager?.Show(new Avalonia.Controls.Notifications.Notification("Error", "Native Notifications are not quite ready. Coming soon.", NotificationType.Error));
            });

            YesCommand = MiniCommand.Create(() =>
            {
                NotificationManager?.Show(new Avalonia.Controls.Notifications.Notification("Avalonia Notifications", "Start adding notifications to your app today."));
            });

            NoCommand = MiniCommand.Create(() =>
            {
                NotificationManager?.Show(new Avalonia.Controls.Notifications.Notification("Avalonia Notifications", "Start adding notifications to your app today. To find out more visit..."));
            });
        }

        public string? Title { get; set; }
        public string? Message { get; set; }

        public MiniCommand YesCommand { get; }

        public MiniCommand NoCommand { get; }

        public MiniCommand ShowCustomManagedNotificationCommand { get; }

        public MiniCommand ShowManagedNotificationCommand { get; }

        public MiniCommand ShowNativeNotificationCommand { get; }

    }
}
