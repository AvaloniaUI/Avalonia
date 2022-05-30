using System.IO;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IPlatformIconLoader
    {
        IWindowIconImpl LoadIcon(string fileName);
        IWindowIconImpl LoadIcon(Stream stream);
        IWindowIconImpl LoadIcon(IBitmapImpl bitmap);
    }
}
