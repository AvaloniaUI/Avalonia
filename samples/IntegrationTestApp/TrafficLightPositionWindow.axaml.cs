using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace IntegrationTestApp;

public partial class TrafficLightPositionWindow : Window
{
    public TrafficLightPositionWindow()
    {
        InitializeComponent();
    }

    private void SetCustomTrafficLightPosition_Click(object? sender, RoutedEventArgs e) =>
        MacOSProperties.SetTrafficLightPosition(this, new Point(70, 40));

    private void ResetTrafficLightPosition_Click(object? sender, RoutedEventArgs e) =>
        MacOSProperties.SetTrafficLightPosition(this, null);

    private void ToggleTrafficLightAppearance_Click(object? sender, RoutedEventArgs e) =>
        RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;

    private void ResizeTrafficLightWindow_Click(object? sender, RoutedEventArgs e)
    {
        Width += 20;
        Height += 20;
    }
}
