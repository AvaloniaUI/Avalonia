using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class SplitViewPage : ContentPage
    {
        public SplitViewPage()
        {
            InitializeComponent();
            DataContext = new SplitViewPageViewModel();
        }
    }
}
