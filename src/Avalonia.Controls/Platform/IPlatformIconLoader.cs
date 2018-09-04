using System.IO;

namespace Avalonia.Platform
{
    public interface IPlatformIconLoader
    {
        IWindowIconImpl LoadIcon(string fileName);
        IWindowIconImpl LoadIcon(Stream stream);
        IWindowIconImpl LoadIcon(IBitmapImpl bitmap);
    }
}
