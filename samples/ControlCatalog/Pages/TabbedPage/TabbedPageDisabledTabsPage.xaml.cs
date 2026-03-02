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
    }
}
