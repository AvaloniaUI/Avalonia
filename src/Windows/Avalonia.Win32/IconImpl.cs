using System;
using System.Drawing;
using System.IO;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    class IconImpl : IWindowIconImpl
    {
        private readonly Icon icon;

        public IconImpl(Icon icon)
        {
            this.icon = icon;
        }

        public IntPtr HIcon => icon.Handle;

        public void Save(Stream outputStream)
        {
            icon.Save(outputStream);
        }
    }
}
