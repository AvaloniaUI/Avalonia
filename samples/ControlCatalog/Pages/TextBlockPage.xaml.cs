using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class TextBlockPage : UserControl
    {
        public TextBlockPage()
        {
            this.InitializeComponent();
            DataContext = "a databound run";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
