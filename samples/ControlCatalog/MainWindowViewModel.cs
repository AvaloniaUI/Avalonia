using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using Avalonia.Controls;
using ReactiveUI;

namespace ControlCatalog
{
    public class MainWindowViewModel : ReactiveObject
    {
        private WindowState _windowState;

        public MainWindowViewModel()
        {
            _windowState = WindowState.Maximized;

            MaximizeCommand = ReactiveCommand.Create(() =>
            {
                WindowState = WindowState.Maximized;
            });

            RestoreCommand = ReactiveCommand.Create(() =>
            {
                WindowState = WindowState.Normal;
            });
        }

        public WindowState WindowState
        {
            get { return _windowState; }
            set { this.RaiseAndSetIfChanged(ref _windowState, value); }
        }

        public ReactiveCommand<Unit, Unit> MaximizeCommand { get; }

        public ReactiveCommand<Unit, Unit> RestoreCommand { get; }
    }
}
