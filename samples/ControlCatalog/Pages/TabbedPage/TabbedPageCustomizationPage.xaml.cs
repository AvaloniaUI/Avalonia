using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageCustomizationPage : UserControl
    {
        private static readonly StreamGeometry HomeGeometry = StreamGeometry.Parse("M10,20V14H14V20H19V12H22L12,3L2,12H5V20H10Z");
        private static readonly StreamGeometry SearchGeometry = StreamGeometry.Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z");
        private static readonly StreamGeometry SettingsGeometry = StreamGeometry.Parse("M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.34 19.43,11L21.54,9.37C21.73,9.22 21.78,8.95 21.66,8.73L19.66,5.27C19.54,5.05 19.27,4.96 19.05,5.05L16.56,6.05C16.04,5.66 15.5,5.32 14.87,5.07L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.07C8.5,5.32 7.96,5.66 7.44,6.05L4.95,5.05C4.73,4.96 4.46,5.05 4.34,5.27L2.34,8.73C2.21,8.95 2.27,9.22 2.46,9.37L4.57,11C4.53,11.34 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.78 2.21,15.05 2.34,15.27L4.34,18.73C4.46,18.95 4.73,19.04 4.95,18.95L7.44,17.94C7.96,18.34 8.5,18.68 9.13,18.93L9.5,21.58C9.54,21.82 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.93C15.5,18.67 16.04,18.34 16.56,17.94L19.05,18.95C19.27,19.04 19.54,18.95 19.66,18.73L21.66,15.27C21.78,15.05 21.73,14.78 21.54,14.63L19.43,12.97Z");

        private static readonly IBrush?[] BarBackgrounds =
        {
            null,
            new SolidColorBrush(Color.Parse("#1565C0")),
            new SolidColorBrush(Color.Parse("#2E7D32")),
            new SolidColorBrush(Color.Parse("#6A1B9A")),
            new SolidColorBrush(Color.Parse("#212121"))
        };

        private static readonly IBrush?[] SelectedColors =
        {
            null,
            new SolidColorBrush(Color.Parse("#E53935")),
            new SolidColorBrush(Color.Parse("#F57C00")),
            new SolidColorBrush(Color.Parse("#00796B")),
            new SolidColorBrush(Color.Parse("#E91E63"))
        };

        private static readonly IBrush?[] UnselectedColors =
        {
            null,
            new SolidColorBrush(Color.Parse("#757575")),
            new SolidColorBrush(Color.Parse("#BDBDBD")),
            new SolidColorBrush(Color.FromArgb(153, 255, 255, 255))
        };

        public TabbedPageCustomizationPage()
        {
            InitializeComponent();
        }

        private void OnPlacementChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoTabs == null) return;
            DemoTabs.TabPlacement = PlacementCombo.SelectedIndex switch
            {
                1 => TabPlacement.Bottom,
                2 => TabPlacement.Left,
                3 => TabPlacement.Right,
                _ => TabPlacement.Top
            };
        }

        private void OnBarBgChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoTabs != null)
                DemoTabs.BarBackground = BarBackgrounds[BarBgCombo.SelectedIndex];
        }

        private void OnSelectedColorChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoTabs != null)
                DemoTabs.SelectedTabBrush = SelectedColors[SelectedColorCombo.SelectedIndex];
        }

        private void OnUnselectedColorChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoTabs != null)
                DemoTabs.UnselectedTabBrush = UnselectedColors[UnselectedColorCombo.SelectedIndex];
        }

        private void OnKeyboardChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoTabs != null)
                DemoTabs.IsKeyboardNavigationEnabled = KeyboardCheck.IsChecked == true;
        }

        private void OnShowIconsChanged(object? sender, RoutedEventArgs e)
        {
            bool show = ShowIconsCheck.IsChecked == true;
            SetIcon(HomePage, show ? HomeGeometry : null);
            SetIcon(SearchPage, show ? SearchGeometry : null);
            SetIcon(SettingsPage, show ? SettingsGeometry : null);
        }

        private static void SetIcon(ContentPage page, StreamGeometry? geometry)
        {
            page.Icon = geometry;
        }

        private void OnTabEnabledChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb || int.TryParse(cb.Tag?.ToString(), out int index) is false)
                return;

            if (DemoTabs.Pages is IList pages && pages[index] is ContentPage page)
                TabbedPage.SetIsTabEnabled(page, cb.IsChecked == true);
        }
    }
}
