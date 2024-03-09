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
            if (e.FilterState is not string { Length: > 0 } searchText)
            {
                e.Accept = true;
            }
            else
            {
                var item = (ItemModel)e.Item!;
                e.Accept = item.IsFavorite || item.ID.ToString().Contains(searchText);
            }
        }

        private void SelectItemId(object? sender, ComparableSorter.ComparableSelectEventArgs e) => e.Comparable = ((ItemModel)e.Item!).ID;
        private void SelectItemIsFavorite(object? sender, ComparableSorter.ComparableSelectEventArgs e) => e.Comparable = ((ItemModel)e.Item!).IsFavorite;
    }
}
