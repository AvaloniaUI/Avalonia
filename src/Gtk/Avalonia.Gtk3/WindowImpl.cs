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
                Native.GtkWindowSetTitle(GtkWidget, t);
        }

        class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
                
            }
        }

        public IDisposable ShowDialog()
        {
            Native.GtkWindowSetModal(GtkWidget, true);
            Show();
            return new EmptyDisposable();
        }

        public void SetSystemDecorations(bool enabled) => Native.GtkWindowSetDecorated(GtkWidget, enabled);

        public void SetIcon(IWindowIconImpl icon) => Native.GtkWindowSetIcon(GtkWidget, (Pixbuf) icon);

        public WindowImpl() : base(Native.GtkWindowNew(GtkWindowType.TopLevel))
        {
        }

        public void SetCoverTaskbarWhenMaximized(bool enable)
        {
            //Why do we even have that?
        }
    }
}
