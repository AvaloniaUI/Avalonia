using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(5000);

                NotificationManager.Instance.Show(new NotificationViewModel { Title = "Warning", Message = "Please save your work before closing." }, "Main");

                await Task.Delay(1500);
                NotificationManager.Instance.Show(new NotificationContent { Message = "Test2", Type = NotificationType.Error }, "Main");

                await Task.Delay(2000);
                NotificationManager.Instance.Show(new NotificationContent { Message = "Test3", Type = NotificationType.Warning }, "Main");

                await Task.Delay(2500);
                NotificationManager.Instance.Show(new NotificationContent { Message = "Test4", Type = NotificationType.Success }, "Main");

                await Task.Delay(500);
                NotificationManager.Instance.Show("Test5", "Main");

            });
        }
    }
}
