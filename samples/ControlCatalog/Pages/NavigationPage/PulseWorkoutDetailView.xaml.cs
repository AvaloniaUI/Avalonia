using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public partial class PulseWorkoutDetailView : UserControl
{
    public Action? BackRequested { get; set; }

    public PulseWorkoutDetailView() => InitializeComponent();

    void OnBackClicked(object? sender, RoutedEventArgs e) =>
        BackRequested?.Invoke();
}
