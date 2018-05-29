using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.DesignerSupport.TestApp
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
