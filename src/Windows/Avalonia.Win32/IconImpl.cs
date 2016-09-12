using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NOT_NETSTANDARD
using System.Drawing;
#endif

namespace Avalonia.Win32
{
    class IconImpl : IWindowIconImpl
    {
#if NOT_NETSTANDARD
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
#endif
    }
}
