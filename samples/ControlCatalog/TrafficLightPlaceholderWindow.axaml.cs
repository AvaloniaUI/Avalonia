using Avalonia;
using Avalonia.Controls;

namespace ControlCatalog
{
    public partial class TrafficLightPlaceholderWindow : Window
    {
        public TrafficLightPlaceholderWindow()
        {
            InitializeComponent();

            foreach (var slider in new[] { PlaceholderWidth, PlaceholderHeight, HorizontalOffset, VerticalOffset })
                slider.ValueChanged += (_, _) => UpdatePlaceholder();

            CustomSize.IsCheckedChanged += (_, _) => UpdatePlaceholder();

            UpdatePlaceholder();
        }

        private void UpdatePlaceholder()
        {
            var isCustomSize = CustomSize.IsChecked == true;
            TrafficLightHost.Width = isCustomSize ? PlaceholderWidth.Value : double.NaN;
            TrafficLightHost.Height = isCustomSize ? PlaceholderHeight.Value : double.NaN;
            TrafficLightHost.Margin = new Thickness(HorizontalOffset.Value, VerticalOffset.Value, 0, 0);
        }
    }
}
