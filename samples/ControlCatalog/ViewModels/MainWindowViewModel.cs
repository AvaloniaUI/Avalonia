using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Threading;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(5000);

                NotificationManager.Show(new NotificationViewModel (NotificationManager) { Title = "Warning", Message = "Did you know that Avalonia now supports Notifications?" });

                await Task.Delay(1500);
                NotificationManager.Show(new NotificationContent { Title= "Title", Message = "Test2", Type = NotificationType.Error });

                await Task.Delay(2000);
                NotificationManager.Show(new NotificationContent { Title = "Title", Message = "Test3", Type = NotificationType.Warning });

                await Task.Delay(2500);
                NotificationManager.Show(new NotificationContent { Title = "Title", Message = "Test4", Type = NotificationType.Success });

                await Task.Delay(2500);
                NotificationManager.Show(new NotificationContent { Title = "Title", Message = "Test5", Type = NotificationType.Information });

                await Task.Delay(500);
                NotificationManager.Show("Test5");

            });
        }

        private INotificationManager _notificationManager;

        public INotificationManager NotificationManager
        {
            get { return _notificationManager; }
            set { this.RaiseAndSetIfChanged(ref _notificationManager, value); }
        }
    }
}
