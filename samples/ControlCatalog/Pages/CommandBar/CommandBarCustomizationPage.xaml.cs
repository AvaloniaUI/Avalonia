using Avalonia.Controls;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class CommandBarCustomizationPage : UserControl
    {
        public CommandBarCustomizationPage()
        {
            InitializeComponent();
        }

        private void OnBgPresetChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (LiveBar == null)
                return;

            if (BgPresetCombo.SelectedItem is not ComboBoxItem { Tag: string preset })
            {
                LiveBar.ClearValue(BackgroundProperty);
                return;
            }

            switch (preset)
            {
                case "Gradient":
                    LiveBar.Background = new LinearGradientBrush
                    {
                        StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                        EndPoint = new Avalonia.RelativePoint(1, 0, Avalonia.RelativeUnit.Relative),
                        GradientStops =
                        {
                            new GradientStop(Color.Parse("#3F51B5"), 0),
                            new GradientStop(Color.Parse("#E91E63"), 1)
                        }
                    };
                    break;
                case "Transparent":
                    LiveBar.Background = Brushes.Transparent;
                    break;
                default:
                    LiveBar.Background = new SolidColorBrush(Color.Parse(preset));
                    break;
            }
        }

        private void OnFgChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (LiveBar == null)
                return;

            if (FgCombo.SelectedItem is ComboBoxItem { Tag: string color })
            {
                LiveBar.Foreground = new SolidColorBrush(Color.Parse(color));
                return;
            }

            LiveBar.ClearValue(ForegroundProperty);
        }

        private void OnRadiusChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (LiveBar == null)
                return;
            
            var r = (int)RadiusSlider.Value;
            LiveBar.CornerRadius = new Avalonia.CornerRadius(r);
            RadiusLabel.Text = $"{r}";
        }

        private void OnBorderChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (LiveBar == null)
                return;
            
            var t = (int)BorderSlider.Value;
            LiveBar.BorderThickness = new Avalonia.Thickness(t);
            BorderLabel.Text = $"{t}";
            if (t > 0)
                LiveBar.BorderBrush = Brushes.Gray;
            else
                LiveBar.BorderBrush = null;
        }
    }
}
