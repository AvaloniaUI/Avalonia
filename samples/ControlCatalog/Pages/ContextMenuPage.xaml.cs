using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class ContextMenuPage : UserControl
    {
        public ContextMenuPage()
        {
            this.InitializeComponent();
            DataContext = new ContextMenuPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
