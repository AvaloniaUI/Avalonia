using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class ListBoxPage : TabbedPage
    {
        public ListBoxPage()
        {
            InitializeComponent();
            DataContext = new ListBoxPageViewModel();
            SelectionPage.DataContext = DataContext;
        }
    }
}
