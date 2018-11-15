using Avalonia;
using Avalonia.Markup.Xaml;

namespace DataGridSample
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
