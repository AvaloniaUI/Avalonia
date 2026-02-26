using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ContentPagePage : UserControl
    {
        public ContentPagePage()
        {
            InitializeComponent();
        }

        private void OnBackgroundChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (SamplePage == null) return;
            SamplePage.Background = BackgroundCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Colors.LightGray),
                2 => new SolidColorBrush(Colors.LightBlue),
                3 => new SolidColorBrush(Colors.LightGreen),
                _ => null
            };
        }

        private void OnHAlignChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (SamplePage == null) return;
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
            if (SamplePage == null) return;
            SamplePage.VerticalContentAlignment = VAlignCombo.SelectedIndex switch
            {
                0 => VerticalAlignment.Top,
                1 => VerticalAlignment.Center,
                2 => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Stretch
            };
        }

        private void OnTopBarChanged(object? sender, RoutedEventArgs e)
        {
            if (SamplePage == null) return;
            SamplePage.TopCommandBar = TopBarCheck.IsChecked == true
                ? new CommandBar
                {
                    Background = new SolidColorBrush(Color.Parse("#E0E0E0")),
                    PrimaryCommands =
                    {
                        new AppBarButton { Label = "Save" },
                        new AppBarButton { Label = "Share" },
                        new AppBarSeparator(),
                        new AppBarToggleButton { Label = "Bold" }
                    }
                }
                : null;
        }

        private void OnBottomBarChanged(object? sender, RoutedEventArgs e)
        {
            if (SamplePage == null) return;
            SamplePage.BottomCommandBar = BottomBarCheck.IsChecked == true
                ? new CommandBar
                {
                    Background = new SolidColorBrush(Color.Parse("#E0E0E0")),
                    PrimaryCommands =
                    {
                        new AppBarButton { Label = "New" },
                        new AppBarButton { Label = "Delete" }
                    }
                }
                : null;
        }

        private void OnSafeAreaChanged(object? sender, RoutedEventArgs e)
        {
            if (SamplePage != null)
                SamplePage.AutomaticallyApplySafeAreaPadding = SafeAreaCheck.IsChecked == true;
        }
    }
}
