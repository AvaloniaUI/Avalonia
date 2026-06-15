using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class ListBoxPage : ContentPage
    {
        public ListBoxPage()
        {
            InitializeComponent();
            DataContext = new ListBoxPageViewModel();
        }
    }
}
