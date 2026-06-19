using Avalonia.Controls;

namespace ControlCatalog.Pages
{
    public partial class TabbedPagePlacementPage : UserControl
    {
        public TabbedPagePlacementPage()
        {
            InitializeComponent();
        }

        private void OnPlacementChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DemoTabs == null) return;

            if (TopRadio?.IsChecked == true)
                DemoTabs.TabPlacement = TabPlacement.Top;
            else if (BottomRadio?.IsChecked == true)
                DemoTabs.TabPlacement = TabPlacement.Bottom;
            else if (LeftRadio?.IsChecked == true)
                DemoTabs.TabPlacement = TabPlacement.Left;
            else if (RightRadio?.IsChecked == true)
                DemoTabs.TabPlacement = TabPlacement.Right;
        }
    }
}
