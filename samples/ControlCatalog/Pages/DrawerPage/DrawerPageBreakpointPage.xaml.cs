using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class DrawerPageBreakpointPage : UserControl
    {
        private bool _isLoaded;

        public DrawerPageBreakpointPage()
        {
            InitializeComponent();
        }

        private void OnControlLoaded(object? sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            DemoDrawer.PropertyChanged += OnDrawerPropertyChanged;
            UpdateStatus();
        }

        private void OnControlUnloaded(object? sender, RoutedEventArgs e)
        {
            DemoDrawer.PropertyChanged -= OnDrawerPropertyChanged;
        }

        private void OnDrawerPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == DrawerPage.BoundsProperty)
                UpdateStatus();
        }

        private void OnBreakpointChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            var value = (int)e.NewValue;
            DemoDrawer.DrawerBreakpointLength = value;
            BreakpointText.Text = value.ToString();
            UpdateStatus();
        }

        private void OnLayoutChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerLayoutBehavior = LayoutCombo.SelectedIndex switch
            {
                0 => DrawerLayoutBehavior.Split,
                1 => DrawerLayoutBehavior.CompactInline,
                2 => DrawerLayoutBehavior.CompactOverlay,
                _ => DrawerLayoutBehavior.Split
            };
            UpdateStatus();
        }

        private void OnMenuItemClick(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded || sender is not Button button)
                return;
            var item = button.Tag?.ToString() ?? "Home";
            DetailTitleText.Text = item;
            DetailPage.Header = item;
            if (DemoDrawer.DrawerLayoutBehavior != DrawerLayoutBehavior.Split)
                DemoDrawer.IsOpen = false;
        }

        private void UpdateStatus()
        {
            var isVertical = DemoDrawer.DrawerPlacement == DrawerPlacement.Top ||
                             DemoDrawer.DrawerPlacement == DrawerPlacement.Bottom;
            var length = isVertical ? DemoDrawer.Bounds.Height : DemoDrawer.Bounds.Width;
            var breakpoint = DemoDrawer.DrawerBreakpointLength;
            WidthText.Text = $"{(isVertical ? "Height" : "Width")}: {(int)length} px";
            var isOverlay = breakpoint > 0 && length > 0 && length < breakpoint;
            ModeText.Text = isOverlay ?
                "Mode: Overlay (below breakpoint)" :
                $"Mode: {DemoDrawer.DrawerLayoutBehavior} (above breakpoint)";
        }
    }
}
