using Avalonia.Controls;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageKeyboardPage : UserControl
    {
        public TabbedPageKeyboardPage()
        {
            InitializeComponent();
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

        private void OnKeyboardChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DemoTabs != null)
                DemoTabs.IsKeyboardNavigationEnabled = KeyboardCheck.IsChecked == true;
        }

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e)
        {
            if (StatusText != null)
                StatusText.Text = $"Selected: {(e.CurrentPage as ContentPage)?.Header} ({DemoTabs.SelectedIndex})";
        }
    }
}
