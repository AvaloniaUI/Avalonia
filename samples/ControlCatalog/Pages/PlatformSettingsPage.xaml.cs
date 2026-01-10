using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class PlatformSettingsPage : UserControl
    {
        public PlatformSettingsPage()
        {
            InitializeComponent();
            DataContext = new PlatformSettingsViewModel();
        }
    }
}

