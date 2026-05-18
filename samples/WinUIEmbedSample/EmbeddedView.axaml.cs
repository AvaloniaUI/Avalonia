using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

namespace WinUIEmbedSample;

public partial class EmbeddedView : UserControl
{
    private int _clicks;

    public EmbeddedView()
    {
        InitializeComponent();
        AvSlider.PropertyChanged += OnSliderPropertyChanged;
    }

    private void OnAvButtonClick(object? sender, RoutedEventArgs e)
    {
        AvClickCount.Text = $"Clicked {++_clicks} times";
    }

    private void OnSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
            AvSliderValue.Text = $"Slider: {AvSlider.Value:F0}";
    }
}
