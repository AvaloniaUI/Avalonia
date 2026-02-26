using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class CommandBarPage : UserControl
    {
        private int _dynamicCommandCount;
        private bool _isLoaded;

        public CommandBarPage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            _isLoaded = true;
        }

        private void OnLabelPositionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (sender is ComboBox combo)
            {
                MainCommandBar.DefaultLabelPosition = combo.SelectedIndex switch
                {
                    0 => CommandBarDefaultLabelPosition.Bottom,
                    1 => CommandBarDefaultLabelPosition.Right,
                    2 => CommandBarDefaultLabelPosition.Collapsed,
                    _ => CommandBarDefaultLabelPosition.Bottom
                };
            }
        }

        private void OnOverflowChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (sender is ComboBox combo)
            {
                MainCommandBar.OverflowButtonVisibility = combo.SelectedIndex switch
                {
                    0 => CommandBarOverflowButtonVisibility.Auto,
                    1 => CommandBarOverflowButtonVisibility.Visible,
                    2 => CommandBarOverflowButtonVisibility.Collapsed,
                    _ => CommandBarOverflowButtonVisibility.Auto
                };
            }
        }

        private void OnIsStickyToggled(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            if (sender is CheckBox check)
                MainCommandBar.IsSticky = check.IsChecked == true;
        }

        private void OnDynamicOverflowToggled(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            if (sender is CheckBox check)
                MainCommandBar.IsDynamicOverflowEnabled = check.IsChecked == true;
        }

        private void OnAddCommand(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            _dynamicCommandCount++;
            var button = new AppBarButton
            {
                Label = $"Cmd {_dynamicCommandCount}",
                Icon = new PathIcon
                {
                    Data = StreamGeometry.Parse(
                        "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z")
                }
            };
            MainCommandBar.PrimaryCommands.Add(button);
        }

        private void OnRemoveCommand(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            if (MainCommandBar.PrimaryCommands.Count > 0)
                MainCommandBar.PrimaryCommands.RemoveAt(MainCommandBar.PrimaryCommands.Count - 1);
        }
    }
}
