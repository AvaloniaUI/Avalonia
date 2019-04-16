using System.Reactive;
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
                NotificationManager.Instance.Show("Notification Accepted", "Main");
            });
        }

        public string Title { get; set; }
        public string Message { get; set; }

        public ReactiveCommand<Unit, Unit> OKCommand { get; }

    }
}
