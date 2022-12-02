using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class WindowInsetsPage : Page
    {
        public WindowInsetsPage()
        {
            this.InitializeComponent();

            DataContext = new PlatformInsetsPageViewModel(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
