using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class TreeViewPage : ContentPage
    {
        public TreeViewPage()
        {
            InitializeComponent();
            DataContext = new TreeViewPageViewModel();
        }
    }
}
