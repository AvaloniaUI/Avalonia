using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public partial class PulseHomeView : UserControl
{
    public Action? WorkoutDetailRequested { get; set; }

    public PulseHomeView() => InitializeComponent();

    void OnRecCard1Pressed(object? sender, PointerPressedEventArgs e) =>
        WorkoutDetailRequested?.Invoke();

    void OnRecCard2Pressed(object? sender, PointerPressedEventArgs e) =>
        WorkoutDetailRequested?.Invoke();

    void OnRecCard3Pressed(object? sender, PointerPressedEventArgs e) =>
        WorkoutDetailRequested?.Invoke();

    void OnPlayButtonClicked(object? sender, RoutedEventArgs e) =>
        WorkoutDetailRequested?.Invoke();
}
