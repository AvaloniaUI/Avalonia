using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Dialogs;
using Avalonia.Platform;
using Avalonia.Reactive;
using System;
using System.ComponentModel.DataAnnotations;
using Avalonia;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        private WindowState _windowState;
        private WindowState[] _windowStates = Array.Empty<WindowState>();
        private ExtendClientAreaChromeHints _chromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
        private bool _extendClientAreaEnabled;
        private bool _systemTitleBarEnabled;
        private bool _preferSystemChromeEnabled;
        private double _titleBarHeight;
        private bool _isSystemBarVisible;
        private bool _displayEdgeToEdge;
        private Thickness _safeAreaPadding;

        public MainWindowViewModel()
        {
            AboutCommand = MiniCommand.CreateFromTask(async () =>
            {
                var dialog = new AboutAvaloniaDialog();

                if ((App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow is { } mainWindow)
                {
                    await dialog.ShowDialog(mainWindow);
                }
            });
            ExitCommand = MiniCommand.Create(() =>
            {
                (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
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

        public bool IsSystemBarVisible
        {
            get { return _isSystemBarVisible; }
            set { this.RaiseAndSetIfChanged(ref _isSystemBarVisible, value); }
        }

        public bool DisplayEdgeToEdge
        {
            get { return _displayEdgeToEdge; }
            set { this.RaiseAndSetIfChanged(ref _displayEdgeToEdge, value); }
        }
        
        public Thickness SafeAreaPadding
        {
            get { return _safeAreaPadding; }
            set { this.RaiseAndSetIfChanged(ref _safeAreaPadding, value); }
        }

        public MiniCommand AboutCommand { get; }

        public MiniCommand ExitCommand { get; }

        private DateTime? _validatedDateExample;

        /// <summary>
        ///    A required DateTime which should demonstrate validation for the DateTimePicker
        /// </summary>
        [Required]
        public DateTime? ValidatedDateExample
        {
            get => _validatedDateExample;
            set => this.RaiseAndSetIfChanged(ref _validatedDateExample, value);
        }
    }
}
