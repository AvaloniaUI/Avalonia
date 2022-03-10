namespace Avalonia.Platform
{
    public interface IWindowingPlatform
    {
        IWindowImpl CreateWindow();

        IWindowImpl CreateEmbeddableWindow();

        ITrayIconImpl? CreateTrayIcon();
    }
}
