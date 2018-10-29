﻿using System.IO;
using Avalonia.Platform;

namespace Avalonia.iOS
{
    class PlatformIconLoader : IPlatformIconLoader
    {
        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream);
                return LoadIcon(stream);
            }
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return new FakeIcon(stream);
        }

        public IWindowIconImpl LoadIcon(string fileName)
        {
            using (var file = File.Open(fileName, FileMode.Open))
            {
                return new FakeIcon(file);
            }
        }
    }

    // Stores the icon created as a stream to support saving even though an icon is never shown
    public class FakeIcon : IWindowIconImpl
    {
        private readonly Stream stream = new MemoryStream();

        public FakeIcon(Stream stream)
        {
            stream.CopyTo(this.stream);
        }

        public void Save(Stream outputStream)
        {
            stream.CopyTo(outputStream);
        }
    }
}
