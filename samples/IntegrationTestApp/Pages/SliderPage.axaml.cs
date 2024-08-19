using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IntegrationTestApp.Pages;

public partial class SliderPage : UserControl
{
    public SliderPage()
    {
        InitializeComponent();
    }

    private void ResetSliders_Click(object? sender, RoutedEventArgs e)
    {
        HorizontalSlider.Value = 50;
    }
}
