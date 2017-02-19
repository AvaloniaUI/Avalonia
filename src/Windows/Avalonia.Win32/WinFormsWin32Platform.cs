using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    partial class Win32Platform
    {
        public IWindowIconImpl LoadIcon(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return CreateImpl(stream);
            }
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return CreateImpl(stream);
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream);
                return new IconImpl(new System.Drawing.Bitmap(memoryStream));
            }
        }

        private static IconImpl CreateImpl(Stream stream)
        {
            try
            {
                return new IconImpl(new System.Drawing.Icon(stream));
            }
            catch (ArgumentException)
            {
                return new IconImpl(new System.Drawing.Bitmap(stream));
            }
        }
    }
}
