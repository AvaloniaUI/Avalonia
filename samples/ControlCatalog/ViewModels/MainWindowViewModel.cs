using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Dialogs;
using ReactiveUI;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel : ReactiveObject
    {
        private readonly INotificationManager _nativeNotificationManager;
        private IManagedNotificationManager _notificationManager;

        public MainWindowViewModel(IManagedNotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
            _nativeNotificationManager = AvaloniaLocator.Current.GetService<INotificationManager>();

            ShowCustomManagedNotificationCommand = ReactiveCommand.Create(() =>
            {
                NotificationManager.Show(new NotificationViewModel(NotificationManager) { Title = "Hey There!", Message = "Did you know that Avalonia now supports Custom In-Window Notifications?" });
            });

            ShowManagedNotificationCommand = ReactiveCommand.Create(() =>
            {
                NotificationManager.Show(new Notification("Welcome", "Avalonia now supports Notifications.", NotificationType.Information));
            });

            ShowNativeNotificationCommand = ReactiveCommand.Create(() =>
            {
                if (_nativeNotificationManager != null)
                {
                    _nativeNotificationManager.Show(new Notification(
                        "Native",
                        "Native Notifications are finally here!",
                        NotificationType.Success,
                        onClick: NativeNotficationClicked,
                        onClose: NativeNotificationClosed));
                }
                else
                {
                    NotificationManager.Show(new Notification(
                        "Native",
                        "Native Notifications are not supported on this platform!",
                        NotificationType.Error));
                }
            });

            AboutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialog = new AboutAvaloniaDialog();

                var mainWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

                await dialog.ShowDialog(mainWindow);
            });

            ExitCommand = ReactiveCommand.Create(() =>
            {
                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown();
            });
        }

        private static void NativeNotificationClosed()
        {
            Console.WriteLine("Native notification closed.");
        }

        private static void NativeNotficationClicked()
        {
            Console.WriteLine("Native notification clicked.");
        }

        public IManagedNotificationManager NotificationManager
        {
            get { return _notificationManager; }
            set { this.RaiseAndSetIfChanged(ref _notificationManager, value); }
        }

        public ReactiveCommand<Unit, Unit> ShowCustomManagedNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowManagedNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowNativeNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> AboutCommand { get; }

        public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    }
}
