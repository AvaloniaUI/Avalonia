﻿using System;
using Avalonia.Controls;
using Avalonia.Platform;
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using Avalonia.Threading;

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
            
            Window.DidResize += delegate
            {
                var windowState = Window.IsMiniaturized ? WindowState.Minimized
                    : (IsZoomed ? WindowState.Maximized : WindowState.Normal);

                if (windowState != _lastWindowState)
                {
                    _lastWindowState = windowState;
                    WindowStateChanged?.Invoke(windowState);
                }
            };
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