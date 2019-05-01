using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Threading;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            this.WhenAnyValue(x => x.NotificationManager).Subscribe(x =>
            {

            });

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(5000);


                NotificationManager.Show(new NotificationViewModel { Title = "Warning", Message = "Please save your work before closing." });

                await Task.Delay(1500);
                NotificationManager.Show(new NotificationContent { Message = "Test2", Type = NotificationType.Error });

                await Task.Delay(2000);
                NotificationManager.Show(new NotificationContent { Message = "Test3", Type = NotificationType.Warning });

                await Task.Delay(2500);
                NotificationManager.Show(new NotificationContent { Message = "Test4", Type = NotificationType.Success });

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
