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
        public IWindowIconImpl LoadIcon(string fileName) => new IconImpl();

        public IWindowIconImpl LoadIcon(Stream stream) => new IconImpl();

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap) => new IconImpl();
    }
}
