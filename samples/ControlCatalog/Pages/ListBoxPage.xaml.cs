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

        private void FilterItem(object? sender, FunctionItemFilter.FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                e.Accept = true;
            }
            else
            {
                var item = (ItemModel)e.Item!;
                e.Accept = item.IsFavorite || item.ID.ToString().Contains(SearchBox.Text!);
            }
        }
    }
}
