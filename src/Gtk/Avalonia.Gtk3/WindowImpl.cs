﻿using System;
using Avalonia.Controls;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;
using System.Runtime.InteropServices;

namespace Avalonia.Gtk3
{
    class WindowImpl : WindowBaseImpl, IWindowImpl
    {
        public WindowImpl() : base(Native.GtkWindowNew(GtkWindowType.TopLevel))
        {
        }

        public void SetTitle(string title)
        {
            using (var t = new Utf8Buffer(title))
                Native.GtkWindowSetTitle(GtkWidget, t);
        }

        public WindowState WindowState
        {
            get
            {
                var state = Native.GdkWindowGetState(Native.GtkWidgetGetWindow(GtkWidget));
                if (state.HasFlag(GdkWindowState.Iconified))
                    return WindowState.Minimized;
                if (state.HasFlag(GdkWindowState.Maximized))
                    return WindowState.Maximized;
                return WindowState.Normal;
            }
            set
            {
                var w = Native.GtkWidgetGetWindow(GtkWidget);
                if (value == WindowState.Minimized)
                    Native.GdkWindowIconify(w);
                else if (value == WindowState.Maximized)
                    Native.GdkWindowMaximize(w);
                else
                {
                    Native.GdkWindowUnmaximize(w);
                    Native.GdkWindowDeiconify(w);
                }
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

        public void SetCoverTaskbarWhenMaximized(bool enable)
        {
            //Why do we even have that?
        }

        public void ShowTaskbarIcon(bool value) => Native.GtkWindowSetSkipTaskbarHint(GtkWidget, !value);
        

        class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {

            }
        }
    }
}
