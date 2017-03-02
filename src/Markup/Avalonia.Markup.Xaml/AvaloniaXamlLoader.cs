namespace Avalonia.Markup.Xaml
{
    public class AvaloniaXamlLoader :
#if OMNIXAML
        AvaloniaXamlLoaderOmniXaml
#else
        AvaloniaXamlLoaderPortableXaml
#endif
    {
    }
}