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
            if (sender is CommandBarButton btn)
                StatusText.Text = $"{btn.Label} clicked";
        }

        private void OnToggleChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is CommandBarToggleButton btn)
                StatusText.Text = btn.IsChecked == true
                    ? $"{btn.Label} enabled"
                    : $"{btn.Label} disabled";
        }
    }
}
