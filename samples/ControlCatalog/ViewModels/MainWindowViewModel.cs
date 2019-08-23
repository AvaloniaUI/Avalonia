using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Notifications;
using Avalonia.Notifications.Managed;
using Avalonia.Notifications.Native;
using ReactiveUI;
using Notification = Avalonia.Notifications.Notification;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel : ReactiveObject
    {
        private string _nativeInfo;

        public MainWindowViewModel(
            IManagedNotificationManager notificationManager,
            INativeNotificationManager nativeNotificationManager
        )
        {
            ShowCustomManagedNotificationCommand = ReactiveCommand.Create(() =>
            {
                notificationManager.Show(new NotificationViewModel(notificationManager)
                {
                    Title = "Hey There!",
                    Message = "Did you know that Avalonia now supports Custom In-Window Notifications?"
                });
            });

            ShowManagedNotificationCommand = ReactiveCommand.Create(() =>
            {
                notificationManager.Show(new Notification("Welcome", "Avalonia now supports Notifications.",
                    NotificationType.Information));
            });

            ShowNativeNotificationCommand = ReactiveCommand.Create(async () =>
            {
                //TODO: Change
                await nativeNotificationManager.ShowAsync(
                    new Notification(
                        "Native Notifications",
                        "Fluid and natural native notifications",
                        NotificationType.Information,
                        expiration: TimeSpan.FromSeconds(7)
                    )
                );
            });

            nativeNotificationManager.GetServerInfoAsync()
                .ToObservable()
                .Subscribe(si => NativeServerInfo = si.ToString());

            NativeServerCapabilities = nativeNotificationManager.GetCapabilitiesAsync()
                .ToObservable();
        }

        public string NativeServerInfo
        {
            get { return _nativeInfo; }
            private set { this.RaiseAndSetIfChanged(ref _nativeInfo, value); }
        }

        public IObservable<string[]> NativeServerCapabilities { get; set; }

        public ReactiveCommand<Unit, Unit> ShowCustomManagedNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowManagedNotificationCommand { get; }

        public ReactiveCommand<Unit, Task> ShowNativeNotificationCommand { get; }
    }
}
