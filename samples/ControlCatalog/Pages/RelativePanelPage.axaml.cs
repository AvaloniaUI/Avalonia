using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class RelativePanelPage : UserControl
    {
        public RelativePanelPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
