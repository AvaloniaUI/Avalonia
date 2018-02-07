using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an icon for a window.
    /// </summary>
    public class WindowIcon
    {
        public WindowIcon(IBitmap bitmap)
        {
            PlatformImpl = AvaloniaLocator.Current.GetService<IPlatformIconLoader>().LoadIcon(bitmap.PlatformImpl.Item);
        }

        public WindowIcon(string fileName)
        {
            PlatformImpl = AvaloniaLocator.Current.GetService<IPlatformIconLoader>().LoadIcon(fileName);
        }

        public WindowIcon(Stream stream)
        {
            PlatformImpl = AvaloniaLocator.Current.GetService<IPlatformIconLoader>().LoadIcon(stream);
        }

        public IWindowIconImpl PlatformImpl { get; }

        public void Save(Stream stream) => PlatformImpl.Save(stream);
    }
}
