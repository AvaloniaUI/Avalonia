using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Threading;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;

            ShowCustomManagedNotificationCommand = ReactiveCommand.Create(() =>
            {
                NotificationManager.Show(new NotificationViewModel(NotificationManager) { Title = "Hey There!", Message = "Did you know that Avalonia now supports Custom In-Window Notifications?" });
            });

            ShowManagedNotificationCommand = ReactiveCommand.Create(() =>
            {
                NotificationManager.Show(new NotificationContent { Title = "Welcome", Message = "Avalonia now supports Notifications.", Type = NotificationType.Information });
            });

            ShowNativeNotificationCommand = ReactiveCommand.Create(() =>
            {
                NotificationManager.Show(new NotificationContent { Title = "Error", Message = "Native Notifications are not quite ready. Coming soon.", Type = NotificationType.Error });
            });
        }

        private INotificationManager _notificationManager;

        public INotificationManager NotificationManager
        {
            get { return _notificationManager; }
            set { this.RaiseAndSetIfChanged(ref _notificationManager, value); }
        }

        public ReactiveCommand<Unit, Unit> ShowCustomManagedNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowManagedNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowNativeNotificationCommand { get; }
    }
}
