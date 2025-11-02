using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class PlatformInfoPage : UserControl
    {
        public PlatformInfoPage()
        {
            InitializeComponent();
            DataContext = new PlatformInformationViewModel();
        }
    }
}
