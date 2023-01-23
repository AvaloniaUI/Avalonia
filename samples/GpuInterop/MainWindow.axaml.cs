using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GpuInterop
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            this.Renderer.DrawFps = true;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
