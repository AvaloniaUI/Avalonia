using Avalonia;
using Avalonia.Markup.Xaml;

namespace BindingDemo
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
