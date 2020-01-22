using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NativeEmbedSample
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            this.AttachDevTools();
        }
    }
}
