using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IWindowingPlatform
    {
        IWindowImpl CreateWindow();

        IWindowImpl CreateEmbeddableWindow();

        ITrayIconImpl? CreateTrayIcon();
    }
}
