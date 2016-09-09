using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32
{
    class IconImpl : IWindowIconImpl
    {
        private Bitmap bitmap;
        private Icon icon;
#if NOT_NETSTANDARD
        public IconImpl(Bitmap bitmap)
        {
            this.bitmap = bitmap;
        }

        public IconImpl(Icon icon)
        {
            this.icon = icon;
        }

    }
}
