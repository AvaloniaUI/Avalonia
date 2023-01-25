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
            PlatformImpl = AvaloniaLocator.Current.GetRequiredService<IPlatformIconLoader>().LoadIcon(bitmap.PlatformImpl.Item);
        }

        public WindowIcon(string fileName)
        {
            PlatformImpl = AvaloniaLocator.Current.GetRequiredService<IPlatformIconLoader>().LoadIcon(fileName);
        }

        public WindowIcon(Stream stream)
        {
            PlatformImpl = AvaloniaLocator.Current.GetRequiredService<IPlatformIconLoader>().LoadIcon(stream);
        }

        public IWindowIconImpl PlatformImpl { get; }

        public void Save(Stream stream) => PlatformImpl.Save(stream);
    }
}
