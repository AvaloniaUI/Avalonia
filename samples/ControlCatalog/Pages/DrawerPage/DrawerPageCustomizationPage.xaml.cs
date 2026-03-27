using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class DrawerPageCustomizationPage : UserControl
    {
        private bool _isLoaded;

        private static readonly string[] _iconPaths =
        {
            // 0 - 3 lines (default hamburger)
            "M3 17h18a1 1 0 0 1 .117 1.993L21 19H3a1 1 0 0 1-.117-1.993L3 17h18H3Zm0-6 18-.002a1 1 0 0 1 .117 1.993l-.117.007L3 13a1 1 0 0 1-.117-1.993L3 11l18-.002L3 11Zm0-6h18a1 1 0 0 1 .117 1.993L21 7H3a1 1 0 0 1-.117-1.993L3 5h18H3Z",
            // 1 - 2 lines
            "M3,13H21V11H3M3,6V8H21V6",
            // 2 - 4 squares
            "M3,11H11V3H3M3,21H11V13H3M13,21H21V13H13M13,3V11H21V3",
        };

        public DrawerPageCustomizationPage()
        {
            InitializeComponent();
            EnableMouseSwipeGesture(DemoDrawer);
        }

        private void OnControlLoaded(object? sender, RoutedEventArgs e)
        {
            _isLoaded = true;
        }

        private void OnToggleDrawer(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.IsOpen = !DemoDrawer.IsOpen;
        }

        private void OnBehaviorChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerBehavior = BehaviorCombo.SelectedIndex switch
            {
                0 => DrawerBehavior.Auto,
                1 => DrawerBehavior.Flyout,
                2 => DrawerBehavior.Locked,
                3 => DrawerBehavior.Disabled,
                _ => DrawerBehavior.Auto
            };
        }

        private void OnLayoutChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerLayoutBehavior = LayoutCombo.SelectedIndex switch
            {
                0 => DrawerLayoutBehavior.Overlay,
                1 => DrawerLayoutBehavior.Split,
                2 => DrawerLayoutBehavior.CompactOverlay,
                3 => DrawerLayoutBehavior.CompactInline,
                _ => DrawerLayoutBehavior.Overlay
            };
        }

        private void OnPlacementChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerPlacement = PlacementCombo.SelectedIndex switch
            {
                1 => DrawerPlacement.Right,
                2 => DrawerPlacement.Top,
                3 => DrawerPlacement.Bottom,
                _ => DrawerPlacement.Left
            };
        }

        private void OnGestureToggled(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (sender is CheckBox check)
                DemoDrawer.IsGestureEnabled = check.IsChecked == true;
        }

        private void OnDrawerLengthChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerLength = e.NewValue;
            DrawerLengthText.Text = ((int)e.NewValue).ToString();
        }

        private void OnDrawerBgChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerBackground = DrawerBgCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Colors.SlateBlue),
                2 => new SolidColorBrush(Colors.DarkCyan),
                3 => new SolidColorBrush(Colors.DarkRed),
                4 => new SolidColorBrush(Colors.DarkGreen),
                _ => null
            };
        }

        private void OnHeaderBgChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerHeaderBackground = HeaderBgCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Colors.DodgerBlue),
                2 => new SolidColorBrush(Colors.Orange),
                3 => new SolidColorBrush(Colors.Teal),
                4 => new SolidColorBrush(Colors.Purple),
                _ => null
            };
        }

        private void OnFooterBgChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerFooterBackground = FooterBgCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Colors.DimGray),
                2 => new SolidColorBrush(Colors.DarkSlateBlue),
                3 => new SolidColorBrush(Colors.DarkOliveGreen),
                4 => new SolidColorBrush(Colors.Maroon),
                _ => null
            };
        }

        private void OnIconChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerIcon = Geometry.Parse(_iconPaths[IconCombo.SelectedIndex]);
        }

        private void OnBackdropChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.BackdropBrush = BackdropCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Color.FromArgb(102, 0, 0, 0)),
                2 => new SolidColorBrush(Color.FromArgb(179, 0, 0, 0)),
                3 => new SolidColorBrush(Color.FromArgb(102, 255, 255, 255)),
                _ => null
            };
        }

        private void OnShowHeaderToggled(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (ShowHeaderCheck.IsChecked == true)
                DemoDrawer.DrawerHeader = DrawerHeaderBorder;
            else
                DemoDrawer.DrawerHeader = null;
        }

        private void OnShowFooterToggled(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (ShowFooterCheck.IsChecked == true)
                DemoDrawer.DrawerFooter = DrawerFooterBorder;
            else
                DemoDrawer.DrawerFooter = null;
        }

        private void OnMenuItemClick(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (sender is not Button button) return;
            var item = button.Tag?.ToString() ?? "Home";

            DetailTitleText.Text = item;

            if (DemoDrawer.DrawerBehavior != DrawerBehavior.Locked)
                DemoDrawer.IsOpen = false;
        }

        private static void EnableMouseSwipeGesture(Control control)
        {
            var recognizer = control.GestureRecognizers
                .OfType<SwipeGestureRecognizer>()
                .FirstOrDefault();

            if (recognizer is not null)
                recognizer.IsMouseEnabled = true;
        }
    }
}
