using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable, PrivateApi]
    public interface IWindowingPlatform
    {
        IWindowImpl CreateWindow();

        IWindowImpl CreateEmbeddableWindow();

        ITrayIconImpl? CreateTrayIcon();
    }
}
