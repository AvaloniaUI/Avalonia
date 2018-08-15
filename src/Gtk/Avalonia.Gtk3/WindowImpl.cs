using System;
using Avalonia.Controls;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    class WindowImpl : WindowBaseImpl, IWindowImpl
    {
        private WindowState _lastWindowState;

        public WindowImpl() : base(Native.GtkWindowNew(GtkWindowType.TopLevel))
        {
        }

        internal override void OnStateChanged(GdkWindowState changed_mask, GdkWindowState new_window_state)
        {
            var windowState = new_window_state.HasFlag(GdkWindowState.Iconified) ? WindowState.Minimized
                : (new_window_state.HasFlag(GdkWindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);

            if (windowState != _lastWindowState)
            {
                _lastWindowState = windowState;
                WindowStateChanged?.Invoke(windowState);
            }

            base.OnStateChanged(changed_mask, new_window_state);
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
                if (value == WindowState.Minimized)
                    Native.GtkWindowIconify(GtkWidget);
                else if (value == WindowState.Maximized)
                    Native.GtkWindowMaximize(GtkWidget);
                else
                {
                    Native.GtkWindowUnmaximize(GtkWidget);
                    Native.GtkWindowDeiconify(GtkWidget);
                }
            }
        }

        public Action<WindowState> WindowStateChanged { get; set; }

        public IDisposable ShowDialog()
        {
            //Native.GtkWindowSetModal(GtkWidget, true);
            EventManager.EnterModal(this);
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

        public void CanResize(bool value) => Native.GtkWindowSetResizable(GtkWidget, value);


        class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {

            }
        }
    }
}
