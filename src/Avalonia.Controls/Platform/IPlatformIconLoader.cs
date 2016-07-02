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
        IIconImpl LoadIcon(string fileName);
        IIconImpl LoadIcon(Stream stream);
        IIconImpl LoadIcon(IBitmapImpl bitmap);
    }
}
