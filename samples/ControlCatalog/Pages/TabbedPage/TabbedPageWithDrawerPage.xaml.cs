using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageWithDrawerPage : UserControl
    {
        public TabbedPageWithDrawerPage()
        {
            InitializeComponent();
        }

        private void OnSectionSelected(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string section)
            {
                SectionText.Text = section;
                DemoDrawer.IsOpen = false;
            }
        }
    }
}
