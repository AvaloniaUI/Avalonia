using System;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.Views
{
    internal class ConsoleView : UserControl
    {
        private readonly ListBox _historyList;
        private readonly TextBox _input;

        public ConsoleView()
        {
            this.InitializeComponent();
            _historyList = this.FindControl<ListBox>("historyList");
            ((ILogical)_historyList).LogicalChildrenChanged += HistoryChanged;
            _input = this.FindControl<TextBox>("input");
            _input.KeyDown += InputKeyDown;
        }

        public void FocusInput() => _input.Focus();

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void HistoryChanged(object? sender, EventArgs e)
        {
            var history = (ILogical)_historyList;

            if (history.LogicalChildrenCount > 0)
            {
                var control = (IControl)history.GetLogicalChild(history.LogicalChildrenCount - 1);
                DispatcherTimer.RunOnce(control.BringIntoView, TimeSpan.Zero);
            }
        }

        private void InputKeyDown(object? sender, KeyEventArgs e)
        {
            var vm = (ConsoleViewModel?)DataContext;
            if (vm is null)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                    _ = vm.Execute();
                    e.Handled = true;
                    break;
                case Key.Up:
                    vm.HistoryUp();
                    _input.CaretIndex = _input.Text.Length;
                    e.Handled = true;
                    break;
                case Key.Down:
                    vm.HistoryDown();
                    _input.CaretIndex = _input.Text.Length;
                    e.Handled = true;
                    break;
            }
        }
    }
}
