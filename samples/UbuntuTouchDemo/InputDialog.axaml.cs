using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UbuntuTouchDemo;

public sealed partial class InputDialog : UserControl
{
    public InputDialog()
    {
        InitializeComponent();
    }

    public event EventHandler? CloseRequested;

    internal void FocusInput() => InputField.Focus();
    internal void BringInputIntoView() => InputField.BringIntoView();

    private void OnCloseClick(object? sender, RoutedEventArgs e)
        => CloseRequested?.Invoke(this, EventArgs.Empty);
}
