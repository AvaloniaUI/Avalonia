using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.Notifications;
using Avalonia.Notifications.Managed;
using Avalonia.Notifications.Native;
using ControlCatalog.ViewModels;
using ReactiveUI;
using Notification = Avalonia.Notifications.Notification;

namespace ControlCatalog.Pages
{
    public class NotificationsPage : UserControl
    {
        public NotificationsPage()
        {
            InitializeComponent();

            AttachedToVisualTree += delegate
            {
                var mainWindow = (MainWindow)VisualRoot;
                var notificationArea = new WindowNotificationManager(mainWindow)
                {
                    Position = NotificationPosition.TopRight, MaxItems = 3
                };
                //dunno
                mainWindow.ApplyTemplate();
                notificationArea.ApplyTemplate();

                var nativeNotification = AvaloniaLocator.Current.GetService<INativeNotificationManager>();

                DataContext = new NotificationPageViewModel(notificationArea, nativeNotification);
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private class NotificationPageViewModel : ReactiveObject
        {
            private readonly IManagedNotificationManager _notificationManager;
            private readonly INativeNotificationManager _nativeNotificationManager;
            private string _nativeInfo;
            private bool _isNativeNotificationAvailable;
            private ObservableCollection<string> _nativeServerCapabilities;

            public NotificationPageViewModel(
                IManagedNotificationManager notificationManager,
                INativeNotificationManager nativeNotificationManager
            )
            {
                _notificationManager = notificationManager;
                _nativeNotificationManager = nativeNotificationManager;

                InitializeCommands();

                FetchNativeNotificationServerInformation();
            }

            public bool IsNativeNotificationAvailable
            {
                get { return _isNativeNotificationAvailable; }
                private set { this.RaiseAndSetIfChanged(ref _isNativeNotificationAvailable, value); }
            }

            public string NativeServerInfo
            {
                get { return _nativeInfo; }
                private set { this.RaiseAndSetIfChanged(ref _nativeInfo, value); }
            }

            public ObservableCollection<string> NativeServerCapabilities
            {
                get { return _nativeServerCapabilities; }
                private set { this.RaiseAndSetIfChanged(ref _nativeServerCapabilities, value); }
            }

            public ReactiveCommand<Unit, Unit> ShowCustomManagedNotificationCommand { get; private set; }

            public ReactiveCommand<Unit, Unit> ShowManagedNotificationCommand { get; private set; }

            public ReactiveCommand<Unit, Task> ShowNativeNotificationCommand { get; private set; }

            private void InitializeCommands()
            {
                ShowCustomManagedNotificationCommand = ReactiveCommand.Create(() =>
                {
                    var customNotification = new CustomNotificationViewModel(_notificationManager)
                    {
                        Title = "Hey There!",
                        Message = "Did you know that Avalonia now supports Custom In-Window Notifications?"
                    };
                    try
                    {
                        _notificationManager.Show(customNotification);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });

                ShowManagedNotificationCommand = ReactiveCommand.Create(() =>
                {
                    var notification = new Notification(
                        "Welcome",
                        "Avalonia now supports Notifications.",
                        NotificationType.Information
                    );
                    _notificationManager.Show(notification);
                });

                ShowNativeNotificationCommand = ReactiveCommand.Create(async () =>
                {
                    await _nativeNotificationManager.ShowAsync(
                        new Notification(
                            "Native Notifications",
                            "Fluid and natural native notifications",
                            NotificationType.Information,
                            expiration: TimeSpan.FromSeconds(7)
                        )
                    );
                });
            }

            private void FetchNativeNotificationServerInformation()
            {
                _nativeNotificationManager
                    .IsAvailable()
                    .ContinueWith(t => IsNativeNotificationAvailable = t.Result);

                _nativeNotificationManager
                    .GetServerInfoAsync()
                    .ContinueWith(t => NativeServerInfo = t.Result.ToString());

                _nativeNotificationManager
                    .GetCapabilitiesAsync()
                    .ContinueWith(t =>
                    {
                        var caps = t.Result;

                        Debug.WriteLine("Native capabilities", nameof(NotificationsPage));
                        foreach (var cap in caps)
                            Debug.WriteLine(cap, nameof(NotificationsPage));

                        NativeServerCapabilities = new ObservableCollection<string>(caps);
                    });
            }
        }
    }
}
