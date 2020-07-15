using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Dialogs;
using Avalonia.Platform;
using System;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel : ReactiveObject
    {
        private IManagedNotificationManager _notificationManager;

        private bool _isMenuItemChecked = true;
        private WindowState _windowState;
        private WindowState[] _windowStates;
        private int _transparencyLevel;
        private ExtendClientAreaChromeHints _chromeHints;
        private bool _extendClientAreaEnabled;
        private bool _systemTitleBarEnabled;        
        private bool _preferSystemChromeEnabled;
        private double _titleBarHeight;

        public MainWindowViewModel(IManagedNotificationManager notificationManager)
        {
            _notificationManager = notificationManager;

            ShowCustomManagedNotificationCommand = ReactiveCommand.Create(() =>
            {
                NotificationManager.Show(new NotificationViewModel(NotificationManager) { Title = "Hey There!", Message = "Did you know that Avalonia now supports Custom In-Window Notifications?" });
            });

            ShowManagedNotificationCommand = ReactiveCommand.Create(() =>
            {
                NotificationManager.Show(new Avalonia.Controls.Notifications.Notification("Welcome", "Avalonia now supports Notifications.", NotificationType.Information));
            });

            ShowNativeNotificationCommand = ReactiveCommand.Create(() =>
            {
                NotificationManager.Show(new Avalonia.Controls.Notifications.Notification("Error", "Native Notifications are not quite ready. Coming soon.", NotificationType.Error));
            });

            AboutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialog = new AboutAvaloniaDialog();

                var mainWindow = (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

                await dialog.ShowDialog(mainWindow);
            });

            ExitCommand = ReactiveCommand.Create(() =>
            {
                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown();
            });

            ToggleMenuItemCheckedCommand = ReactiveCommand.Create(() =>
            {
                IsMenuItemChecked = !IsMenuItemChecked;
            });

            WindowState = WindowState.Normal;

            WindowStates = new WindowState[]
            {
                WindowState.Minimized,
                WindowState.Normal,
                WindowState.Maximized,
                WindowState.FullScreen,
            };

            this.WhenAnyValue(x => x.SystemTitleBarEnabled, x=>x.PreferSystemChromeEnabled)
                .Subscribe(x =>
                {
                    var hints = ExtendClientAreaChromeHints.NoChrome | ExtendClientAreaChromeHints.OSXThickTitleBar;

                    if(x.Item1)
                    {
                        hints |= ExtendClientAreaChromeHints.SystemChrome;
                    }

                    if(x.Item2)
                    {
                        hints |= ExtendClientAreaChromeHints.PreferSystemChrome;
                    }

                    ChromeHints = hints;
                });

            SystemTitleBarEnabled = true;            
            TitleBarHeight = -1;
        }        

        public int TransparencyLevel
        {
            get { return _transparencyLevel; }
            set { this.RaiseAndSetIfChanged(ref _transparencyLevel, value); }
        }        

        public ExtendClientAreaChromeHints ChromeHints
        {
            get { return _chromeHints; }
            set { this.RaiseAndSetIfChanged(ref _chromeHints, value); }
        }        

        public bool ExtendClientAreaEnabled
        {
            get { return _extendClientAreaEnabled; }
            set { this.RaiseAndSetIfChanged(ref _extendClientAreaEnabled, value); }
        }        

        public bool SystemTitleBarEnabled
        {
            get { return _systemTitleBarEnabled; }
            set { this.RaiseAndSetIfChanged(ref _systemTitleBarEnabled, value); }
        }        

        public bool PreferSystemChromeEnabled
        {
            get { return _preferSystemChromeEnabled; }
            set { this.RaiseAndSetIfChanged(ref _preferSystemChromeEnabled, value); }
        }        

        public double TitleBarHeight
        {
            get { return _titleBarHeight; }
            set { this.RaiseAndSetIfChanged(ref _titleBarHeight, value); }
        }

        public WindowState WindowState
        {
            get { return _windowState; }
            set { this.RaiseAndSetIfChanged(ref _windowState, value); }
        }

        public WindowState[] WindowStates
        {
            get { return _windowStates; }
            set { this.RaiseAndSetIfChanged(ref _windowStates, value); }
        }

        public IManagedNotificationManager NotificationManager
        {
            get { return _notificationManager; }
            set { this.RaiseAndSetIfChanged(ref _notificationManager, value); }
        }

        public bool IsMenuItemChecked
        {
            get { return _isMenuItemChecked; }
            set { this.RaiseAndSetIfChanged(ref _isMenuItemChecked, value); }
        }

        public ReactiveCommand<Unit, Unit> ShowCustomManagedNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowManagedNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowNativeNotificationCommand { get; }

        public ReactiveCommand<Unit, Unit> AboutCommand { get; }

        public ReactiveCommand<Unit, Unit> ExitCommand { get; }

        public ReactiveCommand<Unit, Unit> ToggleMenuItemCheckedCommand { get; }
    }
}
