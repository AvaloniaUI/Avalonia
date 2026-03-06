using Avalonia.Controls;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageGesturePage : UserControl
    {
        public TabbedPageGesturePage()
        {
            InitializeComponent();
        }

        private void OnGestureEnabledChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DemoTabs != null)
                DemoTabs.IsGestureEnabled = GestureCheck.IsChecked == true;
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
    }
}
