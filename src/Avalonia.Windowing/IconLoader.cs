using System;
using System.IO;
using Avalonia.Platform;

namespace Avalonia.Windowing
{
    public class WindowIcon : IWindowIconImpl
    {
        private IBitmapImpl _bitmap;
        public WindowIcon(IBitmapImpl bitmap) 
        {
            _bitmap = bitmap;
        }

        public void Save(Stream outputStream)
        {
            _bitmap.Save(outputStream);
        }
    }

    public class IconLoader : IPlatformIconLoader
    {
        public IconLoader()
        {
        }

        public IWindowIconImpl LoadIcon(string fileName)
        {
            return new WindowIcon(
                AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().LoadBitmap(fileName));
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return new WindowIcon(
                AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().LoadBitmap(stream));
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            return new WindowIcon(bitmap);
        }
    }
}
