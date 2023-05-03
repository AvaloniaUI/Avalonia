using System.IO;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable, PrivateApi]
    public interface IPlatformIconLoader
    {
        IWindowIconImpl LoadIcon(string fileName);
        IWindowIconImpl LoadIcon(Stream stream);
        IWindowIconImpl LoadIcon(IBitmapImpl bitmap);
    }
}
