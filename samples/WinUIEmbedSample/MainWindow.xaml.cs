using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using global::Avalonia.Styling;
using AvApplication = global::Avalonia.Application;

namespace WinUIEmbedSample
{
    public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
    {
        private int _clicks;

        public MainWindow()
        {
            InitializeComponent();

            App.Lifetime.MainView = new EmbeddedView();
            AvaloniaPanel.Content = App.Lifetime.MainView;
        }

        private void WinUiButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            WinUiClickCount.Text = $"Clicked {++_clicks} times";
        }

        private void WinUiSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (WinUiSliderValue is not null)
                WinUiSliderValue.Text = $"Slider: {e.NewValue:F0}";
        }

        private void WinUiThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = WinUiThemeCombo.SelectedIndex;

            if (Content is FrameworkElement root)
            {
                root.RequestedTheme = selected switch
                {
                    0 => ElementTheme.Light,
                    1 => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }

            if (AvApplication.Current is { } avApp)
            {
                avApp.RequestedThemeVariant = selected switch
                {
                    0 => ThemeVariant.Light,
                    1 => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
            }
        }
    }
}
