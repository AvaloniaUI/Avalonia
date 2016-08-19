using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Avalonia.Platform
{
    public interface IPlatformIconLoader
    {
        IWindowIconImpl LoadIcon(string fileName);
        IWindowIconImpl LoadIcon(Stream stream);
        IWindowIconImpl LoadIcon(IBitmapImpl bitmap);
    }
}
