using System.IO;
using Avalonia.Platform;

namespace Avalonia.Native
{
    // OSX doesn't have a concept of *window* icon. 
    // Icons in the title bar are only shown if there is 
    // an opened file (on disk) associated with the current window
    // see https://stackoverflow.com/a/7038671/2231814
    class IconLoader : IPlatformIconLoader
    {
        class IconStub : IWindowIconImpl
        {
            private readonly IBitmapImpl _bitmap;

            public IconStub(IBitmapImpl bitmap)
            {
                _bitmap = bitmap;
            }

            public void Save(Stream outputStream)
            {
                _bitmap.Save(outputStream);
            }
        }

        public IWindowIconImpl LoadIcon(string fileName)
        {
            return new IconStub(
                AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().LoadBitmap(fileName));
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return new IconStub(
                AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().LoadBitmap(stream));
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return LoadIcon(ms);
        }
    }
}
