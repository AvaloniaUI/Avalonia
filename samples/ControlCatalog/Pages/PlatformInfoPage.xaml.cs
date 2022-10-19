using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class PlatformInfoPage : UserControl
    {
        public PlatformInfoPage()
        {
            this.InitializeComponent();
            DataContext = new PlatformInformationViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
