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
        //TODO: An actual implementation
        public IWindowIconImpl LoadIcon(string fileName)
        {
            //No file IO for netstandard, still waiting for proper net core tooling
            throw new NotSupportedException();
        }

        public IWindowIconImpl LoadIcon(Stream stream) => new IconImpl(stream);

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return new IconImpl(ms);
        }
    }
}
