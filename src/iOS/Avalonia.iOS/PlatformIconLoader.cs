using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Avalonia.iOS
{
    class PlatformIconLoader : IPlatformIconLoader
    {
        public IIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            return null;
        }

        public IIconImpl LoadIcon(Stream stream)
        {
            return null;
        }

        public IIconImpl LoadIcon(string fileName)
        {
            return null;
        }
    }
}
