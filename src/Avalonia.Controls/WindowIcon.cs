using System.IO;
using Avalonia.Logging;
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
            if (AvaloniaLocator.Current.GetService<IPlatformIconLoader>() is { } iconLoader)
            {
                PlatformImpl = iconLoader.LoadIcon(bitmap.PlatformImpl.Item);
            }
            else
            {
                DoLogIfNull();
            }
        }

        public WindowIcon(string fileName)
        {
            if (AvaloniaLocator.Current.GetService<IPlatformIconLoader>() is { } iconLoader)
            {
                PlatformImpl = iconLoader.LoadIcon(fileName);
            }
            else
            {
                DoLogIfNull();
            }
        }

        public WindowIcon(Stream stream)
        {
            if (AvaloniaLocator.Current.GetService<IPlatformIconLoader>() is { } iconLoader)
            {
                PlatformImpl = iconLoader.LoadIcon(stream);
            }
            else
            {
                DoLogIfNull();
            }
        }

        private void DoLogIfNull()
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Platforms)
                ?.Log(this, "Error: Missing IPlatformIconLoader implementation in current platform.");
        }

        public IWindowIconImpl? PlatformImpl { get; }

        public void Save(Stream stream) => PlatformImpl?.Save(stream);
    }
}
