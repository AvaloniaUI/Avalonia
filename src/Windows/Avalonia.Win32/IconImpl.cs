using System;
using System.Drawing;
using System.IO;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class IconImpl : IWindowIconImpl
    {
        private readonly Win32Icon _icon;
        private readonly byte[] _iconData;

        public IconImpl(Win32Icon icon, byte[] iconData)
        {
            _icon = icon;
            _iconData = iconData;
        }

        public IntPtr HIcon => _icon.Handle;

        public void Save(Stream outputStream)
        {
            outputStream.Write(_iconData, 0, _iconData.Length);
        }
    }
}
