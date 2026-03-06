using Avalonia.Controls;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageDisabledTabsPage : UserControl
    {
        public TabbedPageDisabledTabsPage()
        {
            InitializeComponent();
        }

        private void OnTabEnabledChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is not CheckBox cb || int.TryParse(cb.Tag?.ToString(), out int index) is false)
                return;

            if (DemoTabs.Pages is System.Collections.IList pages && pages[index] is ContentPage page)
                TabbedPage.SetIsTabEnabled(page, cb.IsChecked == true);
        }

        private void OnGoToTab(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is not Button btn || !int.TryParse(btn.Tag?.ToString(), out int index))
                return;

            int before = DemoTabs.SelectedIndex;
            DemoTabs.SelectedIndex = index;
            int after = DemoTabs.SelectedIndex;

            if (StatusText != null)
            {
                StatusText.Text = before == after && index != after
                    ? $"Requested tab {index} (disabled) \u2192 stayed on tab {after}"
                    : index != after
                        ? $"Requested tab {index} (disabled) \u2192 skipped to tab {after}"
                        : $"Selected tab {after}";
            }
        }
    }
}
