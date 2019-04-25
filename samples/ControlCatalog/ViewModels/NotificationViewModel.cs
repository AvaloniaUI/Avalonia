using System.Reactive;
using Avalonia;
using Avalonia.Controls.Notifications;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public class NotificationViewModel
    {
        public NotificationViewModel()
        {
            OKCommand = ReactiveCommand.Create(() =>
            {
                Application.Current.MainWindow.LocalNotificationManager.Show("Notification Accepted");
            });
        }

        public string Title { get; set; }
        public string Message { get; set; }

        public ReactiveCommand<Unit, Unit> OKCommand { get; }

    }
}
