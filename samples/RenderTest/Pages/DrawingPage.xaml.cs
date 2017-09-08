using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RenderTest.Pages
{
    public class DrawingPage : UserControl
    {
        public DrawingPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
