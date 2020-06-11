using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RenderDemo.Pages
{
    public class LineBoundsPage : UserControl
    {
        public LineBoundsPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
