using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gdk;

namespace Avalonia.Gtk
{
    class IconImpl : IWindowIconImpl
    {
        public IconImpl(Pixbuf pixbuf)
        {
            Pixbuf = pixbuf;
        }

        public Pixbuf Pixbuf { get; }
    }
}
