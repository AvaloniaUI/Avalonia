using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public partial class PulseLoginView : UserControl
{
    public Action? LoginRequested { get; set; }

    public PulseLoginView() => InitializeComponent();

    void OnLoginClicked(object? sender, RoutedEventArgs e) =>
        LoginRequested?.Invoke();
}
