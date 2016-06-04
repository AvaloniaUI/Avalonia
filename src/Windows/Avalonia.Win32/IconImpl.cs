using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32
{
    class IconImpl : IIconImpl
    {
        public IconImpl(System.Drawing.Icon icon)
        {
            Icon = icon;
        }

        public System.Drawing.Icon Icon { get; }
    }
}
