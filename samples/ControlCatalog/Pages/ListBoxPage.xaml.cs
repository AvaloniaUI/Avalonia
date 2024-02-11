using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class ListBoxPage : UserControl
    {
        public ListBoxPage()
        {
            DataContext = new ListBoxPageViewModel();
            InitializeComponent();
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            ListBox.Items.Filter = string.IsNullOrEmpty(SearchBox.Text) ? null : FilterItem;
        }

        private void FilterItem(object? sender, ItemSourceViewFilterEventArgs e)
        {
            e.Accept = ((ItemModel)e.Item!).ToString().Contains(SearchBox.Text!);
        }
    }
}
