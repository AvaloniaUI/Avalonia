using Avalonia.Markup.Xaml;

namespace Avalonia.Triangle
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}