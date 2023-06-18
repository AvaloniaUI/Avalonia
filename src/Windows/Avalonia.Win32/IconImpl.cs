using System;
using System.Drawing;
using System.IO;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    internal class IconImpl : IWindowIconImpl, IDisposable
    {
        private readonly Icon _smallIcon;
        private readonly Icon _bigIcon;

        private static readonly int s_taskbarIconSize = Win32Platform.WindowsVersion < PlatformConstants.Windows10 ? 32 : 24;

        public IconImpl(Stream smallIcon, Stream bigIcon)
        {
            _smallIcon = ReadFromStream(smallIcon);
            _bigIcon = ReadFromStream(bigIcon);
        }

        public IconImpl(Stream icon)
        {
            _smallIcon = _bigIcon = ReadFromStream(icon);
        }

        private static Icon ReadFromStream(Stream stream)
        {
            if (!stream.CanSeek)
            {
                using var seekableStream = new MemoryStream();
                stream.CopyTo(seekableStream);
                return ReadFromStream(seekableStream);
            }
            
            if (IsIcoFile(stream))
            {
                return new(stream);
            }
            else
            {
                using var bitmap = new Bitmap(stream);
                return Icon.FromHandle(bitmap.GetHicon());
            }
        }

        private static bool IsIcoFile(Stream stream)
        {
            var header = new byte[4];
            var readCount = stream.Read(header, 0, header.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return readCount == header.Length
                // 0010 - ico file header
                && header[0] == 0 && header[1] == 0 && header[2] == 1 && header[3] == 0;
        }

        // GetSystemMetrics returns values scaled for the primary monitor, as of the time at which the process started.
        // This is no good for a per-monitor DPI aware application. GetSystemMetricsForDpi would solve the problem,
        // but is only available in Windows 10 version 1607 and later. So instead, we just hard-code the 96dpi icon sizes.

        public Icon LoadSmallIcon(double scaleFactor) => new(_smallIcon, GetScaledSize(16, scaleFactor));

        public Icon LoadBigIcon(double scaleFactor) => new(_bigIcon, GetScaledSize(s_taskbarIconSize, scaleFactor));

        private static System.Drawing.Size GetScaledSize(int baseSize, double factor)
        {
            var scaled = (int)Math.Ceiling(baseSize * factor);
            return new(scaled, scaled);
        }

        public void Save(Stream outputStream) => _bigIcon.Save(outputStream);

        public void Dispose()
        {
            _smallIcon.Dispose();
            _bigIcon.Dispose();
        }
    }
}
