using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CommandBarOverflowPage : UserControl
    {
        private int _primaryCount;
        private int _secondaryCount;

        public CommandBarOverflowPage()
        {
            InitializeComponent();
        }

        private void OnOverflowVisChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoBar == null)
                return;
            DemoBar.OverflowButtonVisibility = OverflowVisCombo.SelectedIndex switch
            {
                1 => CommandBarOverflowButtonVisibility.Visible,
                2 => CommandBarOverflowButtonVisibility.Collapsed,
                _ => CommandBarOverflowButtonVisibility.Auto
            };
        }

        private void OnIsOpenChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoBar == null)
                return;
            DemoBar.IsOpen = IsOpenCheck.IsChecked == true;
        }

        private void OnIsStickyChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoBar == null)
                return;
            DemoBar.IsSticky = IsStickyCheck.IsChecked == true;
        }

        private void OnAddPrimary(object? sender, RoutedEventArgs e)
        {
            _primaryCount++;
            DemoBar.PrimaryCommands.Add(new AppBarButton { Label = $"Cmd {_primaryCount}" });
        }

        private void OnAddSecondary(object? sender, RoutedEventArgs e)
        {
            _secondaryCount++;
            DemoBar.SecondaryCommands.Add(new AppBarButton { Label = $"Sec {_secondaryCount}" });
        }
    }
}
