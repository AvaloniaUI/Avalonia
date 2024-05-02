using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class IconImpl : IWindowIconImpl, IDisposable
    {
        private readonly Win32Icon _smallIcon;
        private readonly Win32Icon _bigIcon;

        private static readonly int s_taskbarIconSize = Win32Platform.WindowsVersion < PlatformConstants.Windows10 ? 32 : 24;

        public IconImpl(Stream smallIcon, Stream bigIcon)
        {
            _smallIcon = CreateIconImpl(smallIcon);
            _bigIcon = CreateIconImpl(bigIcon);
        }

        public IconImpl(Stream icon)
        {
            _smallIcon = _bigIcon = CreateIconImpl(icon);
        }

        private static Win32Icon CreateIconImpl(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            if (stream is MemoryStream memoryStream)
            {
                var iconData = memoryStream.ToArray();

                return new Win32Icon(iconData);
            }
            else
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);

                ms.Position = 0;

                var iconData = ms.ToArray();

                return new Win32Icon(iconData);
            }
        }

        // GetSystemMetrics returns values scaled for the primary monitor, as of the time at which the process started.
        // This is no good for a per-monitor DPI aware application. GetSystemMetricsForDpi would solve the problem,
        // but is only available in Windows 10 version 1607 and later. So instead, we just hard-code the 96dpi icon sizes.

        public Win32Icon LoadSmallIcon(double scaleFactor) => new(_smallIcon, GetScaledSize(16, scaleFactor));

        public Win32Icon LoadBigIcon(double scaleFactor)
        {
            var targetSize = GetScaledSize(s_taskbarIconSize, scaleFactor);
            var icon = new Win32Icon(_bigIcon, targetSize);

            // The exact size of a taskbar icon in Windows 10 and later is 24px @ 96dpi. But if an ICO file doesn't have
            // that size, 16px can be selected instead. If this happens, fall back to a 32 pixel icon. Windows will downscale it.
            if (s_taskbarIconSize == 24 && icon.Size.Width < targetSize.Width)
            {
                icon.Dispose();
                icon = new(_bigIcon, GetScaledSize(32, scaleFactor));
            }

            return icon;
        }

        private static PixelSize GetScaledSize(int baseSize, double factor)
        {
            var scaled = (int)Math.Ceiling(baseSize * factor);
            return new(scaled, scaled);
        }

        public void Save(Stream outputStream) => _bigIcon.CopyTo(outputStream);

        public void Dispose()
        {
            _smallIcon.Dispose();
            _bigIcon.Dispose();
        }
    }
}
