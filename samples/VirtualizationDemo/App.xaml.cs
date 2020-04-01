using Avalonia;
using Avalonia.Markup.Xaml;

namespace VirtualizationDemo
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
