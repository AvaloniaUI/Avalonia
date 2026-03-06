using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CommandBarFirstLookPage : UserControl
    {
        public CommandBarFirstLookPage()
        {
            InitializeComponent();
        }

        private void OnButtonClick(object? sender, RoutedEventArgs e)
        {
            if (sender is AppBarButton btn)
                StatusText.Text = $"{btn.Label} clicked";
        }

        private void OnToggleChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is AppBarToggleButton btn)
                StatusText.Text = btn.IsChecked == true
                    ? $"{btn.Label} enabled"
                    : $"{btn.Label} disabled";
        }
    }
}
