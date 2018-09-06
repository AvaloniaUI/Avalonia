using System.IO;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    class PlatformIconLoader : IPlatformIconLoader
    {
        public IWindowIconImpl LoadIcon(string fileName) => Pixbuf.NewFromFile(fileName);

        public IWindowIconImpl LoadIcon(Stream stream) => Pixbuf.NewFromStream(stream);

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms);
            return Pixbuf.NewFromBytes(ms.ToArray());
        }
    }
}
