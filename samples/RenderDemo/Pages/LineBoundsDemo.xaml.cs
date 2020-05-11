using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RenderDemo.Pages
{
    public class LineBoundsDemo : UserControl
    {
        public LineBoundsDemo()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
