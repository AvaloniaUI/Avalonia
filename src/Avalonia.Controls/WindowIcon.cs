using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

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
