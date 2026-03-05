using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ContentPageCustomizationPage : UserControl
    {
        public ContentPageCustomizationPage()
        {
            InitializeComponent();
        }

        private void OnBackgroundChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (SamplePage == null)
                return;
            SamplePage.Background = BackgroundCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Color.Parse("#E3F2FD")),
                2 => new SolidColorBrush(Color.Parse("#E8F5E9")),
                3 => new SolidColorBrush(Color.Parse("#F3E5F5")),
                4 => new SolidColorBrush(Color.Parse("#FFF8E1")),
                _ => null
            };
        }

        private void OnHAlignChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (SamplePage == null)
                return;
            SamplePage.HorizontalContentAlignment = HAlignCombo.SelectedIndex switch
            {
                0 => HorizontalAlignment.Left,
                1 => HorizontalAlignment.Center,
                2 => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Stretch
            };
        }

        private void OnVAlignChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (SamplePage == null)
                return;
            SamplePage.VerticalContentAlignment = VAlignCombo.SelectedIndex switch
            {
                0 => VerticalAlignment.Top,
                1 => VerticalAlignment.Center,
                2 => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Stretch
            };
        }

        private void OnPaddingChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (SamplePage == null)
                return;
            var padding = (int)PaddingSlider.Value;
            SamplePage.Padding = new Avalonia.Thickness(padding);
            PaddingLabel.Text = $"{padding} px";
        }
    }
}
