using Avalonia.Markup.Xaml;

namespace Avalonia.DesignerSupport.TestApp
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
