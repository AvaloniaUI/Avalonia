using System;
using Avalonia.Controls;
using Avalonia.Platform;
using MonoMac.AppKit;

namespace Avalonia.MonoMac
{
    class WindowImpl : WindowBaseImpl, IWindowImpl
    {
        bool _decorated = true;

        public WindowImpl()
        {
            UpdateStyle();
        }

        public WindowState WindowState
        {
            get
            {
                if (Window.IsMiniaturized)
                    return WindowState.Minimized;
                return WindowState.Normal;

            }
            set
            {
                if (value == WindowState.Maximized)
                {
                    if (Window.IsMiniaturized)
                        Window.Deminiaturize(Window);
                    if (!Window.IsZoomed)
                        Window.PerformZoom(Window);
                }
                else if (value.HasFlag(WindowState.Minimized))
                    Window.Miniaturize(Window);
                else
                {
                    if (Window.IsMiniaturized)
                        Window.Deminiaturize(Window);
                    if (Window.IsZoomed)
                        Window.IsZoomed = false;
                }
            }
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            //No-OP, see http://stackoverflow.com/a/7038671/2231814
        }

        public void ShowTaskbarIcon(bool value)
        {
            //No-OP, there is no such this as taskbar in OSX
        }

        protected override NSWindowStyle GetStyle()
        {
            if (_decorated)
                return NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Miniaturizable |
                       NSWindowStyle.Titled;
            return NSWindowStyle.Borderless;
        }

        public void SetSystemDecorations(bool enabled)
        {
            _decorated = true;
            UpdateStyle();
        }

        public void SetTitle(string title) => Window.Title = title;

        class ModalDisposable : IDisposable
        {
            readonly WindowImpl _impl;

            public ModalDisposable(WindowImpl impl)
            {
                _impl = impl;
            }

            public void Dispose()
            {
                _impl.Window.OrderOut(_impl.Window);
            }
        }

        public IDisposable ShowDialog()
        {
            //TODO: Investigate how to return immediately. 
            // May be add some magic to our run loop or something
            NSApplication.SharedApplication.RunModalForWindow(Window);
            return new ModalDisposable(this);
        }
    }
}