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
            UpdateArrowKeyLabels();
        }

        private void UpdateArrowKeyLabels()
        {
            if (ArrowKeysHeader == null) return;
            bool vertical = DemoTabs.TabPlacement is TabPlacement.Left or TabPlacement.Right;
            ArrowKeysHeader.Text = vertical ? "Left / Right placement" : "Top / Bottom placement";
            NextKeyText.Text = vertical ? "\u2193" : "\u2192";
            PrevKeyText.Text = vertical ? "\u2191" : "\u2190";
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
