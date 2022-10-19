using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MobileSandbox
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            //Renderer.DrawFps = true;
            //Renderer.DrawDirtyRects = Renderer.DrawFps = true;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
