using System;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using MonoMac.AppKit;
using MonoMac.CoreGraphics;

namespace Avalonia.MonoMac
{
    class WindowImpl : WindowBaseImpl, IWindowImpl
    {
        public bool IsDecorated = true;
        public bool IsResizable = true;
        public CGRect? UndecoratedLastUnmaximizedFrame;

        private WindowState _lastWindowState;

        public WindowImpl()
        {
            // Post UpdateStyle to UIThread otherwise for as yet unknown reason.
            // The window becomes transparent to mouse clicks except a 100x100 square
            // at the top left. (danwalmsley)
            Dispatcher.UIThread.Post(() =>
            {
                UpdateStyle();
            });

            Window.SetCanBecomeKeyAndMain();
        }

        
        protected override void OnResized()
        {
            var windowState = Window.IsMiniaturized ? WindowState.Minimized
                : (IsZoomed ? WindowState.Maximized : WindowState.Normal);

            if (windowState != _lastWindowState)
            {
                _lastWindowState = windowState;
                WindowStateChanged?.Invoke(windowState);
            }
        }

        public WindowState WindowState
        {
            get
            {
                if (Window.IsMiniaturized)
                    return WindowState.Minimized;
                if (IsZoomed)
                    return WindowState.Maximized;
                return WindowState.Normal;
            }
            set
            {
                if (value == WindowState.Maximized)
                {
                    if (Window.IsMiniaturized)
                        Window.Deminiaturize(Window);
                    if (!IsZoomed)
                        DoZoom();
                }
                else if (value.HasFlag(WindowState.Minimized))
                    Window.Miniaturize(Window);
                else
                {
                    if (Window.IsMiniaturized)
                        Window.Deminiaturize(Window);
                    if (IsZoomed)
                        DoZoom();
                }
            }
        }

        public Action<WindowState> WindowStateChanged { get; set; }

        bool IsZoomed => IsDecorated ? Window.IsZoomed : UndecoratedIsMaximized;

        public bool UndecoratedIsMaximized => Window.Frame == Window.Screen.VisibleFrame;

        void DoZoom()
        {
            if (IsDecorated)
                Window.PerformZoom(Window);
            else
            {
                if (!UndecoratedIsMaximized)
                    UndecoratedLastUnmaximizedFrame = Window.Frame;
                Window.Zoom(Window);
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
            var windowStyle = NSWindowStyle.Borderless;

            if (IsDecorated)
                windowStyle |= NSWindowStyle.Closable | NSWindowStyle.Miniaturizable | NSWindowStyle.Titled;

            if (IsResizable)
                windowStyle |= NSWindowStyle.Resizable;

            return windowStyle;
        }

        public void SetSystemDecorations(bool enabled)
        {
            IsDecorated = enabled;
            UpdateStyle();
        }

        public void CanResize(bool value)
        {
            IsResizable = value;
            UpdateStyle();
        }

        public void SetTitle(string title) => Window.Title = title;

        class ModalDisposable : IDisposable
        {
            readonly WindowImpl _impl;
            readonly IntPtr _modalSession;
            bool disposed;

            public ModalDisposable(WindowImpl impl, IntPtr modalSession)
            {
                _impl = impl;
                _modalSession = modalSession;
            }

            public void Continue()
            {
                if (disposed)
                    return;

                var response = (NSRunResponse)NSApplication.SharedApplication.RunModalSession(_modalSession);
                if (response == NSRunResponse.Continues)
                {
                    Dispatcher.UIThread.Post(Continue, DispatcherPriority.ContextIdle);
                }
                else
                {
                    Logging.Logger.Log(Logging.LogEventLevel.Debug, "MonoMac", this, "Modal session ended");
                }
            }

            public void Dispose()
            {
                Logging.Logger.Log(Logging.LogEventLevel.Debug, "MonoMac", this, "ModalDisposable disposed");
                _impl.Window.OrderOut(_impl.Window);
                NSApplication.SharedApplication.EndModalSession(_modalSession);
                disposed = true;
            }
        }

        public IDisposable ShowDialog()
        {
            var session = NSApplication.SharedApplication.BeginModalSession(Window);
            var disposable = new ModalDisposable(this, session);
            Dispatcher.UIThread.Post(disposable.Continue, DispatcherPriority.ContextIdle);

            return disposable;
        }
    }
}
