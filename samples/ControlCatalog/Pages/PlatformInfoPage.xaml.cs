using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class PlatformInfoPage : ContentPage
    {
        public PlatformInfoPage()
        {
            InitializeComponent();
            DataContext = new PlatformInformationViewModel();
        }
    }
}
