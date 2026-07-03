using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RenderDemo.Pages
{
    public class ResizePatternPage : UserControl
    {
        public ResizePatternPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
