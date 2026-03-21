using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Dialogs;
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
        private bool _extendClientAreaEnabled;
        private double _titleBarHeight;
        private bool _isSystemBarVisible;
        private bool _displayEdgeToEdge;
        private Thickness _safeAreaPadding;
        private bool _canResize;
        private bool _canMinimize;
        private bool _canMaximize;

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

            TitleBarHeight = -1;
            CanResize = true;
            CanMinimize = true;
            CanMaximize = true;
        }        
        
        public bool ExtendClientAreaEnabled
        {
            get { return _extendClientAreaEnabled; }
            set { RaiseAndSetIfChanged(ref _extendClientAreaEnabled, value); }
        }

        public double TitleBarHeight
        {
            get { return _titleBarHeight; }
            set { RaiseAndSetIfChanged(ref _titleBarHeight, value); }
        }

        public WindowState WindowState
        {
            get { return _windowState; }
            set { RaiseAndSetIfChanged(ref _windowState, value); }
        }

        public WindowState[] WindowStates
        {
            get { return _windowStates; }
            set { RaiseAndSetIfChanged(ref _windowStates, value); }
        }

        public bool IsSystemBarVisible
        {
            get { return _isSystemBarVisible; }
            set { RaiseAndSetIfChanged(ref _isSystemBarVisible, value); }
        }

        public bool DisplayEdgeToEdge
        {
            get { return _displayEdgeToEdge; }
            set { RaiseAndSetIfChanged(ref _displayEdgeToEdge, value); }
        }
        
        public Thickness SafeAreaPadding
        {
            get { return _safeAreaPadding; }
            set { RaiseAndSetIfChanged(ref _safeAreaPadding, value); }
        }

        public bool CanResize
        {
            get { return _canResize; }
            set { RaiseAndSetIfChanged(ref _canResize, value); }
        }

        public bool CanMinimize
        {
            get { return _canMinimize; }
            set { RaiseAndSetIfChanged(ref _canMinimize, value); }
        }

        public bool CanMaximize
        {
            get { return _canMaximize; }
            set { RaiseAndSetIfChanged(ref _canMaximize, value); }
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
            set => RaiseAndSetIfChanged(ref _validatedDateExample, value);
        }
    }
}
