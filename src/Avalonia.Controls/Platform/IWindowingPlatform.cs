using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable, PrivateApi]
    public interface IWindowingPlatform
    {
        IWindowImpl CreateWindow();

        ITopLevelImpl CreateEmbeddableTopLevel();
        
        IWindowImpl CreateEmbeddableWindow();

        ITrayIconImpl? CreateTrayIcon();
    }
}
