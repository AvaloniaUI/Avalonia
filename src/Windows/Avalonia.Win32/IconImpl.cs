using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32
{
    class IconImpl : IWindowIconImpl
    {
#if NOT_NETSTANDARD
        public IconImpl(System.Drawing.Bitmap iconBitmap)
        {
            IconBitmap = iconBitmap;
        }

        public System.Drawing.Bitmap IconBitmap { get; }
#endif
    }
}
