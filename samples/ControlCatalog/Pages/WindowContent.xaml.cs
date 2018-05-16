using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class WindowContent : Window
    {
        public WindowContent()
        {
            this.InitializeComponent();            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }        
    }
}
