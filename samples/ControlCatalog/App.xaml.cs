using Avalonia;
using Avalonia.Markup.Xaml;

namespace ControlCatalog
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
