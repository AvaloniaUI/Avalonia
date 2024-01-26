using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Wayland
{
    internal class WlIconLoader : IPlatformIconLoader
    {
        public IWindowIconImpl LoadIcon(string fileName) => LoadIcon(new Bitmap(fileName));

        public IWindowIconImpl LoadIcon(Stream stream) => LoadIcon(new Bitmap(stream));

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms);
            ms.Position = 0;
            return LoadIcon(ms);
        }

        private static IWindowIconImpl LoadIcon(IBitmap bitmap)
        {
            var rv = new WlIconData(bitmap);
            bitmap.Dispose();
            return rv;
        }
    }
}
