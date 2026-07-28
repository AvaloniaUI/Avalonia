using global::Avalonia;
using global::Avalonia.Markup.Xaml;
using AvApplication = global::Avalonia.Application;

namespace WinUIEmbedSample;

public partial class AvaloniaApp : AvApplication
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
