using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.DesignerSupport.TestApp
{
    public class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
