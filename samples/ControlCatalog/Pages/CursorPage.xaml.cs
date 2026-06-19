using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class CursorPage : ContentPage
    {
        public CursorPage()
        {
            InitializeComponent();
            DataContext = new CursorPageViewModel();
        }
    }
}
