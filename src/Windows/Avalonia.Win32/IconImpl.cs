using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    class IconImpl : IWindowIconImpl
    {
        private Bitmap bitmap;
        private Icon icon;

        public IconImpl(Bitmap bitmap)
        {
            this.bitmap = bitmap;
        }

        public IconImpl(Icon icon)
        {
            this.icon = icon;
        }

        public IntPtr HIcon => icon?.Handle ?? bitmap.GetHicon();

        public void Save(Stream outputStream)
        {
            if (icon != null)
            {
                icon.Save(outputStream);
            }
            else
            {
                bitmap.Save(outputStream, ImageFormat.Png);
            }
        }
    }
}
