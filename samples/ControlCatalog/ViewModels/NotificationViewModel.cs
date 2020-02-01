using System.Reactive;
using Avalonia.Controls.Notifications;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public class NotificationViewModel
    {
        public NotificationViewModel(INotificationManager manager)
        {
            YesCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await manager.ShowAsync(new Avalonia.Controls.Notifications.Notification("Avalonia Notifications", "Start adding notifications to your app today."));
            });

            NoCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await manager.ShowAsync(new Avalonia.Controls.Notifications.Notification("Avalonia Notifications", "Start adding notifications to your app today. To find out more visit..."));
            });
        }

        public string Title { get; set; }
        public string Message { get; set; }

        public ReactiveCommand<Unit, Unit> YesCommand { get; }

        public ReactiveCommand<Unit, Unit> NoCommand { get; }

    }
}
