using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace IntegrationTestApp;

public class TopmostWindowTest : Window
{
    public TopmostWindowTest(string name)
    {
        Name = name;
        InitializeComponent();
        PositionChanged += (s, e) => this.GetControl<TextBox>("CurrentPosition").Text = $"{Position}";
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Position += new PixelPoint(100, 100);
    }
}
