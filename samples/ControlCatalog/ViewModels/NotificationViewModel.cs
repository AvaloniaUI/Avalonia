using System.Reactive;
using Avalonia.Controls.Notifications;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class NotificationViewModel
    {
        public NotificationViewModel(INotificationManager manager)
        {
            YesCommand = MiniCommand.Create(() =>
            {
                manager.Show(new Avalonia.Controls.Notifications.Notification("Avalonia Notifications", "Start adding notifications to your app today."));
            });

            NoCommand = MiniCommand.Create(() =>
            {
                manager.Show(new Avalonia.Controls.Notifications.Notification("Avalonia Notifications", "Start adding notifications to your app today. To find out more visit..."));
            });
        }

        public string Title { get; set; }
        public string Message { get; set; }

        public MiniCommand YesCommand { get; }

        public MiniCommand NoCommand { get; }

    }
}
