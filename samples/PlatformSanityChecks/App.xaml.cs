using Avalonia;
using Avalonia.Markup.Xaml;

namespace PlatformSanityChecks
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
