using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class AcrylicPage : UserControl
    {
        public AcrylicPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
