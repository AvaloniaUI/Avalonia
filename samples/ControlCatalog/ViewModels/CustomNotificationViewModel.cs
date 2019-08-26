using System.Reactive;
using Avalonia.Notifications;
using ReactiveUI;
using Notification = Avalonia.Notifications.Notification;

namespace ControlCatalog.ViewModels
{
    public class CustomNotificationViewModel
    {
        public CustomNotificationViewModel(INotificationManager manager)
        {
            YesCommand = ReactiveCommand.Create(() =>
            {
                var notification = GetNotification("Start adding notifications to your app today.");
                manager.Show(notification);
            });

            NoCommand = ReactiveCommand.Create(() =>
            {
                var notification = GetNotification(
                    "Learn more about them here: https://github.com/AvaloniaUI/Avalonia/wiki/Notifications"
                );
                manager.Show(notification);
            });
        }

        public string Title { get; set; }
        public string Message { get; set; }

        public ReactiveCommand<Unit, Unit> YesCommand { get; }

        public ReactiveCommand<Unit, Unit> NoCommand { get; }

        private static Notification GetNotification(string message)
        {
            return new Notification(
                "Avalonia Notifications",
                message
            );
        }
    }
}
