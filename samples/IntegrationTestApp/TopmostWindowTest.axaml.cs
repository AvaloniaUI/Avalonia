using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IntegrationTestApp;

public partial class TopmostWindowTest : Window
{
    public TopmostWindowTest(string name)
    {
        Name = name;
        InitializeComponent();
        PositionChanged += (s, e) => CurrentPosition.Text = $"{Position}";
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Position += new PixelPoint(100, 100);
    }
}
