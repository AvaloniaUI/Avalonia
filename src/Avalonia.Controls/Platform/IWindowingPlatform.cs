namespace Avalonia.Platform
{
    public interface IWindowingPlatform
    {
        IWindowImpl CreateWindow();
        IEmbeddableWindowImpl CreateEmbeddableWindow();
    }
}
