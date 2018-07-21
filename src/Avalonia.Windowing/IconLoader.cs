using System;
using System.IO;
using Avalonia.Platform;

namespace Avalonia.Windowing
{
    public class WindowIcon : IWindowIconImpl
    {
        public void Save(Stream outputStream)
        {
        }
    }

    public class IconLoader : IPlatformIconLoader
    {
        public IconLoader()
        {
        }

        public IWindowIconImpl LoadIcon(string fileName)
        {
            return new WindowIcon();
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return new WindowIcon();
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            return new WindowIcon();
        }
    }
}
