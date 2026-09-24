using System;
using System.Windows.Input;
using Avalonia.Interactivity;
using ControlCatalog.Controls;

namespace ControlCatalog.Pages
{
    public partial class ButtonsPage : SamplePage
    {
        private int _clickCount;
        private int _repeatButtonClickCount;
        private int _commandCount;
        private bool _canCount = true;

        public ButtonsPage()
        {
            InitializeComponent();
            CountCommand = new RelayCommand(() =>
            {
                _commandCount++;
                ModeStatus.Text = $"Command executed {_commandCount} time{(_commandCount == 1 ? "" : "s")}.";
            }, () => CanCount);
            DataContext = this;
        }

        public RelayCommand CountCommand { get; }

        public bool CanCount
        {
            get => _canCount;
            set
            {
                _canCount = value;
                CountCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnDemoButtonClick(object? sender, RoutedEventArgs args)
        {
            _clickCount++;
            ClickStatus.Text = $"Click raised {_clickCount} time{(_clickCount == 1 ? "" : "s")}.";
        }

        private void OnModeButtonClick(object? sender, RoutedEventArgs args)
        {
            ModeStatus.Text = $"Click raised with ClickMode.{ModeButton.ClickMode}.";
        }

        private void OnHotKeyButtonClick(object? sender, RoutedEventArgs args)
        {
            ModeStatus.Text = "Click raised by the hot key or the pointer.";
        }

        private void OnRepeatButtonClick(object? sender, RoutedEventArgs args)
        {
            _repeatButtonClickCount++;
            RepeatButtonTextBlock.Text = $"Repeat Button: {_repeatButtonClickCount}";
        }

        public sealed class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;

            public RelayCommand(Action execute, Func<bool> canExecute)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => _canExecute();

            public void Execute(object? parameter) => _execute();

            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
