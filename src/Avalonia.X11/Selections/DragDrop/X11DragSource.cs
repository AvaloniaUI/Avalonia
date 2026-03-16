using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Threading;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Selections.DragDrop;

/// <summary>
/// Implementation of <see cref="IPlatformDragSource"/> for X11 using XDND.
/// Specs: https://www.freedesktop.org/wiki/Specifications/XDND/
/// </summary>
internal sealed class X11DragSource(AvaloniaX11Platform platform) : IPlatformDragSource
{
    public async Task<DragDropEffects> DoDragDropAsync(
        PointerPressedEventArgs triggerEvent,
        IDataTransfer dataTransfer,
        DragDropEffects allowedEffects)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (TopLevel.GetTopLevel(triggerEvent.Source as Visual)?.PlatformImpl is not IXdndWindow window)
            throw new ArgumentOutOfRangeException(nameof(triggerEvent), "Invalid drag source");

        triggerEvent.Pointer.Capture(null);

        var cursorFactory = AvaloniaLocator.Current.GetService<ICursorFactory>();

        using var handler = new Handler(platform, window.Handle, cursorFactory);
        await handler.Completion;

        return DragDropEffects.None;
    }

    private sealed class Handler : IDisposable
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly IntPtr _sourceWindow;
        private readonly TaskCompletionSource _completionSource = new();
        private readonly X11EventDispatcher.EventHandler? _originalEventHandler;
        private XdndTargetInfo? _lastTarget;

        public Task Completion
            => _completionSource.Task;

        public Handler(AvaloniaX11Platform platform, IntPtr sourceWindow, ICursorFactory? cursorFactory)
        {
            _platform = platform;
            _sourceWindow = sourceWindow;

            if (!platform.Windows.TryGetValue(sourceWindow, out _originalEventHandler))
            {
                _completionSource.TrySetResult();
                return;
            }

            // Note: in the standard case (starting a drop-drop operation on pointer pressed), X11 already has an
            // implicit capture. However, the capture is from our child render window when GL is used, instead of
            // the parent window. For now, release any existing capture and start our own.
            // TODO: make the render window invisible from input using XShape.
            XUngrabPointer(platform.Display, 0);

            Console.WriteLine($"Grabbing for window 0x{sourceWindow:x}");

            var grabResult = XGrabPointer(
                platform.Display,
                _sourceWindow,
                false,
                EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask,
                GrabMode.GrabModeAsync,
                GrabMode.GrabModeAsync,
                0,
                platform.Info.DefaultCursor,
                0);

            if (grabResult != GrabResult.GrabSuccess)
            {
                Console.WriteLine($"Could not grab: {grabResult}");

                _completionSource.TrySetResult();
                return;
            }

            Console.WriteLine("Grabbed!");

            _platform.Windows[_sourceWindow] = OnEvent;
        }

        private void OnEvent(ref XEvent xev)
        {
            Console.WriteLine($"Event on drag window: {xev.type}");

            if (xev.type == XEventName.MotionNotify)
            {
                var m = xev.MotionEvent;
                var target = FindXdndTarget(m.x_root, m.y_root);

                Console.WriteLine($"Motion on 0x{m.subwindow:x} at {m.x},{m.y} (root={m.x_root},{m.y_root}); XDND: win=0x{target?.MessageWindow:x}; version={target?.Version}");
            }

            else if (xev.type == XEventName.ButtonRelease)
            {
                Console.WriteLine("Completing");
                _completionSource.TrySetResult();
            }
            else
                _originalEventHandler!(ref xev);
        }

        private XdndTargetInfo? FindXdndTarget(int rootX, int rootY)
        {
            var display = _platform.Display;
            var rootWindow = _platform.Info.RootWindow;
            var currentWindow = rootWindow;

            while (currentWindow != 0)
            {
                if (TryGetXdndTargetInfo(currentWindow) is { } info)
                    return info;

                if (!XTranslateCoordinates(display, rootWindow, currentWindow, rootX, rootY, out _, out _, out var childWindow))
                    return null;

                currentWindow = childWindow;
            }

            return null;
        }

        private XdndTargetInfo? TryGetXdndTargetInfo(IntPtr window)
        {
            var proxyWindow = GetXdndProxyWindow(window);
            var messageWindow = proxyWindow != 0 ? proxyWindow : window;
            var version = GetXdndVersion(messageWindow);

            return version != 0 ? new XdndTargetInfo(version, window, messageWindow) : null;
        }

        private IntPtr GetXdndProxyWindow(IntPtr window)
        {
            var atoms = _platform.Info.Atoms;

            // Spec: If this window property exists, it must be of type XA_WINDOW and must contain the ID of the proxy window
            // that should be checked for XdndAware and that should receive all the client messages, etc.
            var proxyWindow = XGetWindowPropertyAsIntPtr(_platform.Display, window, atoms.XdndProxy, atoms.WINDOW) ?? 0;
            if (proxyWindow == 0)
                return 0;

            // Spec: The proxy window must have the XdndProxy property set to point to itself.
            var proxyOnProxy = XGetWindowPropertyAsIntPtr(_platform.Display, proxyWindow, atoms.XdndProxy, atoms.WINDOW) ?? 0;
            if (proxyOnProxy != proxyWindow)
                return 0;

            return proxyWindow;
        }

        private byte GetXdndVersion(IntPtr window)
        {
            var atoms = _platform.Info.Atoms;

            var version = XGetWindowPropertyAsIntPtr(_platform.Display, window, atoms.XdndAware, atoms.ATOM) ?? 0;
            if (version is < XdndConstants.MinXdndVersion or > byte.MaxValue)
                version = 0;

            return (byte)version;
        }

        private void SendXdndEnter(XdndTargetInfo target)
        {
            var evt = new XEvent
            {
                ClientMessageEvent = new XClientMessageEvent
                {
                    type = XEventName.ClientMessage,
                    display = _platform.Display,
                    window = target.MessageWindow,
                    message_type = _platform.Info.Atoms.XdndEnter,
                    format = 32,
                    ptr1 = target.TargetWindow,
                    ptr2 = 0,
                    ptr3 = 0,
                    ptr4 = 0,
                    ptr5 = 0
                }
            };

            XSendEvent(_platform.Display, target.MessageWindow, false, (IntPtr)EventMask.NoEventMask, ref evt);
            XFlush(_platform.Display);
        }

        public void Dispose()
        {
            XUngrabPointer(_platform.Display, 0);

            if (_originalEventHandler is not null && _platform.Windows.ContainsKey(_sourceWindow))
                _platform.Windows[_sourceWindow] = _originalEventHandler;
        }

        private readonly struct XdndTargetInfo(byte version, IntPtr targetWindow, IntPtr messageWindow)
        {
            public readonly byte Version = version;
            public readonly IntPtr TargetWindow = targetWindow;
            public readonly IntPtr MessageWindow = messageWindow;
        }
    }
}
