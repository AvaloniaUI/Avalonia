using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gdk;
using System.IO;

namespace Avalonia.Gtk
{
    class IconImpl : IWindowIconImpl
    {
        public IconImpl(Pixbuf pixbuf)
        {
            Pixbuf = pixbuf;
        }

        public Pixbuf Pixbuf { get; }

        public void Save(Stream stream)
        {
            var buffer = Pixbuf.SaveToBuffer("png");
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
