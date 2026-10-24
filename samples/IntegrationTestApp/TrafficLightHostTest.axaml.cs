using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IntegrationTestApp;

public partial class TrafficLightHostTest : Window
{
    public TrafficLightHostTest()
    {
        InitializeComponent();
    }

    private void ApplyTrafficLightHost_Click(object? sender, RoutedEventArgs e)
    {
        var size = string.IsNullOrEmpty(TrafficLightHostSize.Text)
            ? new Size(double.NaN, double.NaN)
            : Size.Parse(TrafficLightHostSize.Text);

        TrafficLightHost.Margin = Thickness.Parse(TrafficLightHostMargin.Text!);
        TrafficLightHost.Width = size.Width;
        TrafficLightHost.Height = size.Height;
    }

    private void AddToTrafficLightHostWindowWidth_Click(object? sender, RoutedEventArgs e) => Width += 100;
}
