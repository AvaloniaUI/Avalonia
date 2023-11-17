using Avalonia;
using Avalonia.Markup.Xaml;

namespace AppWithoutLifetime;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
