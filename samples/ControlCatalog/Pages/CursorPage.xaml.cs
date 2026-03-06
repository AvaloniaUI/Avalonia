using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class CursorPage : UserControl
    {
        public CursorPage()
        {
            InitializeComponent();
            DataContext = new CursorPageViewModel();
        }
    }
}
