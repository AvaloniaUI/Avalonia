using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PlatformSanityChecks
{
    public class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
