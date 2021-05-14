using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Dialogs;
using Avalonia.Platform;
using System;
using System.Reactive.Linq;
using Avalonia.Threading;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel : ViewModelBase
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
        private string _test;

        public string Test
        {
            get => _test;
            set => this.RaiseAndSetIfChanged(ref _test, value);
        }

        public MainWindowViewModel()
        {

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (sender, args) => Test = DateTime.Now.ToLongTimeString();
            timer.Start();

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

        public MiniCommand ShowCustomManagedNotificationCommand { get; }

        public MiniCommand ShowManagedNotificationCommand { get; }

        public MiniCommand ShowNativeNotificationCommand { get; }

        public MiniCommand AboutCommand { get; }

        public MiniCommand ExitCommand { get; }

        public MiniCommand ToggleMenuItemCheckedCommand { get; }
    }
}
