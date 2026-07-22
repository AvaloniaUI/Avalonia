using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class DrawerPageRtlPage : UserControl
    {
        public DrawerPageRtlPage()
        {
            InitializeComponent();
        }

        private void OnRtlToggled(object? sender, RoutedEventArgs e)
        {
            if (DemoDrawer == null) return;
            DemoDrawer.FlowDirection = RtlCheckBox.IsChecked == true
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }

        private void OnPlacementChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoDrawer == null) return;
            DemoDrawer.DrawerPlacement = PlacementCombo.SelectedIndex switch
            {
                0 => DrawerPlacement.Left,
                1 => DrawerPlacement.Right,
                _ => DrawerPlacement.Left
            };
        }

        private void OnToggleDrawer(object? sender, RoutedEventArgs e)
        {
            if (DemoDrawer == null) return;
            DemoDrawer.IsOpen = !DemoDrawer.IsOpen;
        }

        private void OnMenuItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            var item = button.Tag?.ToString() ?? "Home";

            DetailTitleText.Text = item;
            DetailDescriptionText.Text = item switch
            {
                "Home" => "Toggle RTL to see the drawer flip to the right edge.\nGestures are mirrored: drag from right edge to open, drag right to close.",
                "Profile" => "View and edit your profile information here.",
                "Messages" => "Your messages and notifications appear here.",
                "Settings" => "Configure application preferences and options.",
                _ => $"Content for {item}"
            };

            DemoDrawer.IsOpen = false;
        }
    }
}
