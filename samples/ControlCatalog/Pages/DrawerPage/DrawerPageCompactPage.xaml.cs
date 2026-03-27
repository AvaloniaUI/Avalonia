using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class DrawerPageCompactPage : UserControl
    {
        private bool _isLoaded;

        public DrawerPageCompactPage()
        {
            InitializeComponent();
        }

        private void OnControlLoaded(object? sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            DemoDrawer.Opened += OnDrawerStatusChanged;
            DemoDrawer.Closed += OnDrawerStatusChanged;
        }

        private void OnControlUnloaded(object? sender, RoutedEventArgs e)
        {
            DemoDrawer.Opened -= OnDrawerStatusChanged;
            DemoDrawer.Closed -= OnDrawerStatusChanged;
        }

        private void OnDrawerStatusChanged(object? sender, System.EventArgs e) => UpdateStatus();

        private void OnLayoutChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerLayoutBehavior = LayoutCombo.SelectedIndex switch
            {
                0 => DrawerLayoutBehavior.CompactOverlay,
                1 => DrawerLayoutBehavior.CompactInline,
                _ => DrawerLayoutBehavior.CompactOverlay
            };
        }

        private void OnCompactLengthChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.CompactDrawerLength = e.NewValue;
            CompactLengthText.Text = ((int)e.NewValue).ToString();
        }

        private void OnDrawerLengthChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerLength = e.NewValue;
            DrawerLengthText.Text = ((int)e.NewValue).ToString();
        }

        private void OnMenuItemClick(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (sender is not Button button)
                return;
            var item = button.Tag?.ToString() ?? "Home";
            DetailTitleText.Text = item;
            DetailPage.Header = item;
            DemoDrawer.IsOpen = false;
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Drawer: {(DemoDrawer.IsOpen ? "Open" : "Closed")}";
        }
    }
}
