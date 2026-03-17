using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Threading;
using static Avalonia.X11.Selections.DragDrop.XdndConstants;
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

        using var handler = new Handler(platform, window.Handle, dataTransfer.ToAsynchronous(), allowedEffects, cursorFactory);
        await handler.Completion;

        return DragDropEffects.None;
    }

    private sealed class Handler : IDisposable
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly IntPtr _sourceWindow;
        private readonly IAsyncDataTransfer _dataTransfer;
        private readonly DragDropEffects _allowedEffects;
        private readonly TaskCompletionSource<DragDropEffects> _completionSource = new();
        private X11EventDispatcher.EventHandler? _originalEventHandler;
        private bool _pointerGrabbed;
        private XdndTargetInfo? _lastTarget;
        private DragDropDataProvider? _dataProvider;
        private IntPtr _lastStatusAction;

        public Handler(
            AvaloniaX11Platform platform,
            IntPtr sourceWindow,
            IAsyncDataTransfer dataTransfer,
            DragDropEffects allowedEffects,
            ICursorFactory? cursorFactory)
        {
            _platform = platform;
            _sourceWindow = sourceWindow;
            _dataTransfer = dataTransfer;
            _allowedEffects = allowedEffects;

            if (!platform.Windows.TryGetValue(sourceWindow, out _originalEventHandler))
            {
                _completionSource.TrySetResult(DragDropEffects.None);
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

                _completionSource.TrySetResult(DragDropEffects.None);
                return;
            }

            Console.WriteLine("Grabbed!");

            _pointerGrabbed = true;
            _platform.Windows[_sourceWindow] = OnEvent;
        }

        public Task<DragDropEffects> Completion
            => _completionSource.Task;

        private IntPtr[] DataFormatAtoms
        {
            get
            {
                return field ??= CalcDataFormatAtoms();

                IntPtr[] CalcDataFormatAtoms()
                {
                    var atomValues = new List<IntPtr>(_dataTransfer.Formats.Count);

                    foreach (var format in _dataTransfer.Formats)
                    {
                        foreach (var atom in DataFormatHelper.ToAtoms(format, _platform.Info.Atoms))
                            atomValues.Add(atom);
                    }

                    return atomValues.ToArray();
                }
            }
        }

        private DragDropDataProvider DataProvider
        {
            get
            {
                return _dataProvider ??= CreateDataProvider();

                DragDropDataProvider CreateDataProvider()
                {
                    var dataProvider = new DragDropDataProvider(_platform, _dataTransfer);
                    var formats = DataFormatAtoms;

                    XChangeProperty(
                        _platform.Display,
                        dataProvider.Window,
                        _platform.Info.Atoms.XdndTypeList,
                        _platform.Info.Atoms.ATOM,
                        32,
                        PropertyMode.Replace,
                        formats,
                        formats.Length);

                    return dataProvider;
                }
            }
        }

        private void OnEvent(ref XEvent xev)
        {
            if (xev.type == XEventName.MotionNotify && _pointerGrabbed)
                OnMotionNotify(in xev.MotionEvent);

            else if (xev.type == XEventName.ButtonRelease && _pointerGrabbed)
                OnButtonRelease(in xev.ButtonEvent);

            else if (xev.type == XEventName.ClientMessage)
            {
                ref var message = ref xev.ClientMessageEvent;
                var atoms = _platform.Info.Atoms;

                if (message.message_type == atoms.XdndStatus)
                    OnXdndStatus(in message);
                else if (message.message_type == atoms.XdndFinished)
                    OnXdndFinished(in message);
                else
                    _originalEventHandler?.Invoke(ref xev);
            }

            else
                _originalEventHandler?.Invoke(ref xev);
        }

        private void OnMotionNotify(in XMotionEvent motion)
        {
            var target = FindXdndTarget(motion.x_root, motion.y_root);

            if (_lastTarget != target)
            {
                if (_lastTarget is { } lastTarget)
                    SendXdndLeave(lastTarget);

                _lastTarget = target;

                if (target is { } newTarget)
                    SendXdndEnter(newTarget);
            }

            if (target is { } currentTarget)
            {
                var action = XdndActionHelper.EffectsToAction(_allowedEffects, _platform.Info.Atoms);
                SendXdndPosition(currentTarget, motion.x_root, motion.y_root, motion.time, action);
            }

            Console.WriteLine($"Motion on 0x{motion.subwindow:x} at {motion.x},{motion.y} (root={motion.x_root},{motion.y_root}); XDND: win=0x{target?.MessageWindow:x}; version={target?.Version}");
        }

        private void OnButtonRelease(in XButtonEvent button)
        {
            UngrabPointer();

            if (_lastTarget is { } lastTarget)
                SendXdndDrop(lastTarget, button.time);
        }

        private void OnXdndStatus(in XClientMessageEvent message)
        {
            Console.WriteLine("Received XdndStatus");

            if (_lastTarget is null)
                return;

            var targetWindow = message.ptr1;
            if (targetWindow == 0 || targetWindow != _dataProvider?.Window)
                return;

            var accepted = (message.ptr2 & 1) == 1;
            _lastStatusAction = accepted ? message.ptr5 : 0;

            Console.WriteLine($"Status updated: {XdndActionHelper.ActionToEffects(_lastStatusAction, _platform.Info.Atoms)}");
        }

        private void OnXdndFinished(in XClientMessageEvent message)
        {
            Console.WriteLine("Received XdndFinished");

            if (_lastTarget is not { } lastTarget)
                return;

            var targetWindow = message.ptr1;
            if (targetWindow == 0 || targetWindow != _dataProvider?.Window)
                return;

            _lastTarget = null;

            IntPtr action;
            if (lastTarget.Version >= 5)
            {
                var accepted = (message.ptr2 & 1) == 1;
                action = accepted ? message.ptr3 : 0;
            }
            else
                action = _lastStatusAction;

            var effects = XdndActionHelper.ActionToEffects(action, _platform.Info.Atoms);
            _completionSource.TrySetResult(effects);
        }

        private XdndTargetInfo? FindXdndTarget(int x, int y)
        {
            var display = _platform.Display;
            var rootWindow = _platform.Info.RootWindow;
            var currentWindow = rootWindow;

            while (currentWindow != 0)
            {
                if (TryGetXdndTargetInfo(currentWindow) is { } info)
                    return info;

                if (!XTranslateCoordinates(display, rootWindow, currentWindow, x, y, out _, out _, out var childWindow))
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
            if (version is < MinXdndVersion or > byte.MaxValue)
                version = 0;

            return (byte)version;
        }

        private void SendXdndEnter(XdndTargetInfo target)
        {
            Console.WriteLine("Sending XdndEnter");

            var version = Math.Min(target.Version, XdndVersion);
            var formats = DataFormatAtoms;
            var hasMoreFormats = formats.Length > 3;

            SendXdndMessage(
                _platform.Info.Atoms.XdndEnter,
                target,
                DataProvider.Window,
                (version << 24) | (hasMoreFormats ? 1 : 0),
                formats.Length >= 1 ? formats[0] : 0,
                formats.Length >= 2 ? formats[1] : 0,
                formats.Length >= 3 ? formats[2] : 0);
        }

        private void SendXdndPosition(XdndTargetInfo target, int x, int y, IntPtr timestamp, IntPtr action)
        {
            Console.WriteLine("Sending XdndPosition");

            SendXdndMessage(
                _platform.Info.Atoms.XdndPosition,
                target,
                DataProvider.Window,
                0,
                (x << 16) | y,
                timestamp,
                action);
        }

        private void SendXdndLeave(XdndTargetInfo target)
        {
            Console.WriteLine("Sending XdndLeave");

            SendXdndMessage(
                _platform.Info.Atoms.XdndLeave,
                target,
                DataProvider.Window,
                0,
                0,
                0,
                0);
        }

        private void SendXdndDrop(XdndTargetInfo target, IntPtr timestamp)
        {
            Console.WriteLine("Sending XdndDrop");

            SendXdndMessage(
                _platform.Info.Atoms.XdndDrop,
                target,
                DataProvider.Window,
                0,
                timestamp,
                0,
                0);
        }

        private void SendXdndMessage(
            IntPtr messageType,
            XdndTargetInfo target,
            IntPtr sourceWindow,
            IntPtr ptr2,
            IntPtr ptr3,
            IntPtr ptr4,
            IntPtr ptr5)
        {
            var evt = new XEvent
            {
                ClientMessageEvent = new XClientMessageEvent
                {
                    type = XEventName.ClientMessage,
                    display = _platform.Display,
                    window = target.MessageWindow,
                    message_type = messageType,
                    format = 32,
                    ptr1 = sourceWindow,
                    ptr2 = ptr2,
                    ptr3 = ptr3,
                    ptr4 = ptr4,
                    ptr5 = ptr5
                }
            };

            XSendEvent(_platform.Display, target.MessageWindow, false, (IntPtr)EventMask.NoEventMask, ref evt);
            XFlush(_platform.Display);
        }

        private void UngrabPointer()
        {
            _pointerGrabbed = false;

            XUngrabPointer(_platform.Display, 0);
            XFlush(_platform.Display);
        }

        public void Dispose()
        {
            if (_pointerGrabbed)
                UngrabPointer();

            if (_originalEventHandler is not null && _platform.Windows.ContainsKey(_sourceWindow))
            {
                _platform.Windows[_sourceWindow] = _originalEventHandler;
                _originalEventHandler = null;
            }

            _lastTarget = null;
            _lastStatusAction = 0;

            _dataProvider?.Dispose();
            _dataProvider = null;
        }

        private readonly record struct XdndTargetInfo(
            byte Version,
            IntPtr TargetWindow,
            IntPtr MessageWindow);
    }
}
