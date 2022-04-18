using System.IO;

using Avalonia.Platform;


namespace Avalonia.Wayland
{
    // 
    public class WlIconLoader : IPlatformIconLoader
    {
        public IWindowIconImpl LoadIcon(string fileName) => null;

        public IWindowIconImpl LoadIcon(Stream stream) => null;

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap) => null;
    }
}
