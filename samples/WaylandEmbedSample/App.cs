using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace WaylandEmbedSample;

// Minimal code-only Application: the embedding host. The auto-hosted GTK toplevel (scenario 2) is wrapped
// into an Avalonia Window by Avalonia.Wayland.Embedding; this app just provides the styling context.
public sealed class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}
