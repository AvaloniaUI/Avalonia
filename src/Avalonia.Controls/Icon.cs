using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an icon for a window.
    /// </summary>
    public class Icon
    {
        public Icon(string fileName)
        {
            PlatformImpl = AvaloniaLocator.Current.GetService<IPlatformIconLoader>().LoadIcon(fileName);
        }

        public Icon(Stream stream)
        {
            PlatformImpl = AvaloniaLocator.Current.GetService<IPlatformIconLoader>().LoadIcon(stream);
        }

        public IIconImpl PlatformImpl { get; }
    }
}
