using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public partial class HotKeyPage : ContentPage
{
    public HotKeyPage()
    {
        IncrementCommand = new DelegateCommand(Increment);
        InitializeComponent();
        DataContext = this;
    }

    public ICommand IncrementCommand { get; }

    private static void Increment(object? parameter)
    {
        if (parameter is Run run && int.TryParse(run.Text, out var value))
        {
            run.Text = (value + 1).ToString();
        }
    }

    private void CountButton_OnClick(object? sender, RoutedEventArgs e) =>
        Increment((sender as Button)?.Tag);

    private void FocusHotKeyButton_OnClick(object? sender, RoutedEventArgs e) => HotKeyButton.Focus();

    private void FocusRoutingButton_OnClick(object? sender, RoutedEventArgs e) => RoutingButton.Focus();

    private void FocusRoutingTextBox_OnClick(object? sender, RoutedEventArgs e) => RoutingTextBox.Focus();

    private void Reset_OnClick(object? sender, RoutedEventArgs e)
    {
        HotKeyCount.Text = "0";
        ButtonACount.Text = "0";
        ButtonCCount.Text = "0";
        ParentACount.Text = "0";
        ParentBCount.Text = "0";
        TextBoxACount.Text = "0";
        TextBoxBCount.Text = "0";
        RootCount.Text = "0";
    }

    private sealed class DelegateCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);
    }
}
