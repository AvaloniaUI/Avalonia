using System;
using Avalonia.Controls;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    class WindowImpl : TopLevelImpl, IWindowImpl
    {
        public WindowState WindowState { get; set; } //STUB
        public void SetTitle(string title)
        {
            using (var t = new Utf8Buffer(title))
                Native.GtkWindowSetTitle(_gtkWidget, t);
        }

        public IDisposable ShowDialog()
        {
            return null;
            //STUB
        }

        public void SetSystemDecorations(bool enabled) => Native.GtkWindowSetDecorated(_gtkWidget, enabled);

        public void SetIcon(IWindowIconImpl icon)
        {
            //STUB
        }

        public WindowImpl() : base(Native.GtkWindowNew(GtkWindowType.TopLevel))
        {
        }

        public override Size ClientSize
        {
            get
            {
                int w, h;
                Native.GtkWindowGetSize(_gtkWidget, out w, out h);
                return new Size(w, h);
            }
            set
            {
                //STUB
            }
        }
    }
}
