using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
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

        var cursorFactory = AvaloniaLocator.Current.GetService<ICursorFactory>() as X11CursorFactory;

        using var handler = new Handler(platform, window.Handle, dataTransfer, allowedEffects, cursorFactory);
        await handler.Completion;

        return DragDropEffects.None;
    }

    private sealed class Handler : IDisposable
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly IntPtr _sourceWindow;
        private readonly IDataTransfer _dataTransfer;
        private readonly DragDropEffects _allowedEffects;
        private readonly X11CursorFactory? _cursorFactory;
        private readonly DragDropDataProvider _dataProvider;
        private readonly TaskCompletionSource<DragDropEffects> _completionSource = new();
        private readonly IntPtr[] _formatAtoms;
        private X11WindowInfo? _originalSourceWindowInfo;
        private bool _pointerGrabbed;
        private XdndTargetInfo? _lastTarget;
        private DragDropEffects _currentEffects;

        public Handler(
            AvaloniaX11Platform platform,
            IntPtr sourceWindow,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects,
            X11CursorFactory? cursorFactory)
        {
            _platform = platform;
            _sourceWindow = sourceWindow;
            _dataTransfer = dataTransfer;
            _allowedEffects = allowedEffects;
            _cursorFactory = cursorFactory;
            _currentEffects = allowedEffects;
            _dataProvider = new DragDropDataProvider(platform, dataTransfer.ToAsynchronous());

            if (!platform.Windows.TryGetValue(sourceWindow, out var sourceWindowInfo))
            {
                _formatAtoms = [];
                _completionSource.TrySetResult(DragDropEffects.None);
                return;
            }

            // Note: in the standard case (starting a drop-drop operation on pointer pressed), X11 already has an
            // implicit capture. However, the capture is from our child render window when GL is used, instead of
            // the parent window. For now, release any existing capture and start our own.
            // TODO: make the render window invisible from input using XShape.
            XUngrabPointer(platform.Display, 0);

            var grabResult = XGrabPointer(
                platform.Display,
                _sourceWindow,
                false,
                EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask,
                GrabMode.GrabModeAsync,
                GrabMode.GrabModeAsync,
                0,
                GetCursor(allowedEffects),
                0);

            if (grabResult != GrabResult.GrabSuccess)
            {
                _formatAtoms = [];
                _completionSource.TrySetResult(DragDropEffects.None);
                return;
            }

            _pointerGrabbed = true;

            // Replace the window event handler with our own during the drag operation
            _originalSourceWindowInfo = sourceWindowInfo;
            _platform.Windows[_sourceWindow] = new X11WindowInfo(OnEvent, sourceWindowInfo.Window);

            var atoms = _platform.Info.Atoms;
            _formatAtoms = DataFormatHelper.ToAtoms(dataTransfer.Formats, atoms);

            XChangeProperty(
                _platform.Display,
                _sourceWindow,
                atoms.XdndTypeList,
                atoms.ATOM,
                32,
                PropertyMode.Replace,
                _formatAtoms,
                _formatAtoms.Length);
        }

        public Task<DragDropEffects> Completion
            => _completionSource.Task;

        private void OnEvent(ref XEvent evt)
        {
            if (evt.type == XEventName.MotionNotify && _pointerGrabbed)
                OnMotionNotify(in evt.MotionEvent);

            else if (evt.type == XEventName.ButtonRelease && _pointerGrabbed)
                OnButtonRelease(in evt.ButtonEvent);

            else if (evt.type == XEventName.ClientMessage)
            {
                ref var message = ref evt.ClientMessageEvent;
                var atoms = _platform.Info.Atoms;

                if (message.message_type == atoms.XdndStatus)
                    OnXdndStatus(in message);
                else if (message.message_type == atoms.XdndFinished)
                    OnXdndFinished(in message);
                else
                    _originalSourceWindowInfo?.EventHandler(ref evt);
            }

            else if (evt.type == XEventName.SelectionRequest)
                _dataProvider.OnSelectionRequest(in evt.SelectionRequestEvent);

            else
                _originalSourceWindowInfo?.EventHandler(ref evt);
        }

        private void OnMotionNotify(in XMotionEvent motion)
        {
            var rootPosition = new PixelPoint(motion.x_root, motion.y_root);
            var target = FindXdndTarget(rootPosition);

            if (_lastTarget != target)
            {
                if (_lastTarget is { } lastTarget)
                {
                    if (lastTarget.InProcessWindow is not null)
                        ProcessRawDragEvent(lastTarget.InProcessWindow, RawDragEventType.DragLeave, rootPosition);
                    else
                        SendXdndLeave(lastTarget);
                }

                _lastTarget = target;
                UpdateCurrentEffects(_allowedEffects);

                if (target is { } newTarget)
                {
                    if (newTarget.InProcessWindow is not null)
                        ProcessRawDragEvent(newTarget.InProcessWindow, RawDragEventType.DragEnter, rootPosition);
                    else
                        SendXdndEnter(newTarget);
                }
            }

            if (target is { } currentTarget)
            {
                if (currentTarget.InProcessWindow is not null)
                    ProcessRawDragEvent(currentTarget.InProcessWindow, RawDragEventType.DragOver, rootPosition);
                else
                {
                    var action = XdndActionHelper.EffectsToAction(_allowedEffects, _platform.Info.Atoms);
                    SendXdndPosition(currentTarget, rootPosition, motion.time, action);
                }
            }
        }

        private void OnButtonRelease(in XButtonEvent button)
        {
            UngrabPointer();

            if (_lastTarget is not { } lastTarget)
                return;

            if (lastTarget.InProcessWindow is not null)
            {
                var rootPosition = new PixelPoint(button.x_root, button.y_root);
                ProcessRawDragEvent(lastTarget.InProcessWindow, RawDragEventType.Drop, rootPosition);
            }
            else
            {
                _dataProvider.SetOwner(_sourceWindow);
                SendXdndDrop(lastTarget, button.time);
            }
        }

        private void OnXdndStatus(in XClientMessageEvent message)
        {
            if (_lastTarget is not { } lastTarget || message.ptr1 != lastTarget.TargetWindow)
                return;

            var accepted = (message.ptr2 & 1) == 1;
            var action = accepted ? message.ptr5 : 0;
            var effects = XdndActionHelper.ActionToEffects(action, _platform.Info.Atoms);
            UpdateCurrentEffects(effects & _allowedEffects);
        }

        private void OnXdndFinished(in XClientMessageEvent message)
        {
            if (_lastTarget is not { } lastTarget || message.ptr1 != lastTarget.TargetWindow)
                return;

            _lastTarget = null;

            if (lastTarget.Version >= 5)
            {
                var accepted = (message.ptr2 & 1) == 1;
                var action = accepted ? message.ptr3 : 0;
                var effects = XdndActionHelper.ActionToEffects(action, _platform.Info.Atoms);
                UpdateCurrentEffects(effects & _allowedEffects);
            }

            _completionSource.TrySetResult(_currentEffects);
        }

        private XdndTargetInfo? FindXdndTarget(PixelPoint rootPosition)
        {
            var display = _platform.Display;
            var rootWindow = _platform.Info.RootWindow;
            var currentWindow = rootWindow;

            while (currentWindow != 0)
            {
                if (TryGetXdndTargetInfo(currentWindow) is { } info)
                    return info;

                if (!XTranslateCoordinates(display, rootWindow, currentWindow, rootPosition.X, rootPosition.Y,
                    out _, out _, out var childWindow))
                {
                    return null;
                }

                currentWindow = childWindow;
            }

            return null;
        }

        private XdndTargetInfo? TryGetXdndTargetInfo(IntPtr window)
        {
            // Special case our own windows: we don't need to go through X for them.
            if (_platform.Windows.TryGetValue(window, out var windowInfo)
                && windowInfo.Window is { } inProcessWindow)
            {
                return new XdndTargetInfo(XdndVersion, window, window, inProcessWindow);
            }

            var proxyWindow = GetXdndProxyWindow(window);
            var messageWindow = proxyWindow != 0 ? proxyWindow : window;
            var version = GetXdndVersion(messageWindow);

            return version != 0 ? new XdndTargetInfo(version, window, messageWindow, null) : null;
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
            var version = Math.Min(target.Version, XdndVersion);
            var hasMoreFormats = _formatAtoms.Length > 3;

            SendXdndMessage(
                _platform.Info.Atoms.XdndEnter,
                target.MessageWindow,
                (version << 24) | (hasMoreFormats ? 1 : 0),
                _formatAtoms.Length >= 1 ? _formatAtoms[0] : 0,
                _formatAtoms.Length >= 2 ? _formatAtoms[1] : 0,
                _formatAtoms.Length >= 3 ? _formatAtoms[2] : 0);
        }

        private void SendXdndPosition(XdndTargetInfo target, PixelPoint rootPosition, IntPtr timestamp, IntPtr action)
        {
            SendXdndMessage(
                _platform.Info.Atoms.XdndPosition,
                target.MessageWindow,
                0,
                (rootPosition.X << 16) | rootPosition.Y,
                timestamp,
                action);
        }

        private void SendXdndLeave(XdndTargetInfo target)
        {
            SendXdndMessage(_platform.Info.Atoms.XdndLeave, target.MessageWindow, 0, 0, 0, 0);
        }

        private void SendXdndDrop(XdndTargetInfo target, IntPtr timestamp)
        {
            SendXdndMessage(_platform.Info.Atoms.XdndDrop, target.MessageWindow, 0, timestamp, 0, 0);
        }

        private void SendXdndMessage(
            IntPtr messageType,
            IntPtr messageWindow,
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
                    window = messageWindow,
                    message_type = messageType,
                    format = 32,
                    ptr1 = _sourceWindow,
                    ptr2 = ptr2,
                    ptr3 = ptr3,
                    ptr4 = ptr4,
                    ptr5 = ptr5
                }
            };

            XSendEvent(_platform.Display, messageWindow, false, (IntPtr)EventMask.NoEventMask, ref evt);
            XFlush(_platform.Display);
        }

        private void ProcessRawDragEvent(X11Window targetWindow, RawDragEventType eventType, PixelPoint rootPosition)
        {
            if (targetWindow.DragDropDevice is not { } dragDropDevice)
            {
                UpdateCurrentEffects(DragDropEffects.None);
                return;
            }

            var localPosition = targetWindow.PointToClient(rootPosition);

            var dragEvent = new RawDragEvent(
                dragDropDevice,
                eventType,
                targetWindow.InputRoot,
                localPosition,
                _dataTransfer,
                _allowedEffects,
                RawInputModifiers.None);

            dragDropDevice.ProcessRawEvent(dragEvent);

            UpdateCurrentEffects(dragEvent.Effects & _allowedEffects);
        }

        private void UngrabPointer()
        {
            _pointerGrabbed = false;

            XUngrabPointer(_platform.Display, 0);
            XFlush(_platform.Display);
        }

        private void UpdateCurrentEffects(DragDropEffects effects)
        {
            if (_currentEffects == effects)
                return;

            _currentEffects = effects;

            if (_pointerGrabbed)
            {
                XChangeActivePointerGrab(
                    _platform.Display,
                    EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask,
                    GetCursor(effects),
                    0);
            }
        }

        private IntPtr GetCursor(DragDropEffects effects)
        {
            if (_cursorFactory is not null)
            {
                if ((effects & DragDropEffects.Copy) != 0)
                    return _cursorFactory.GetCursorHandle(StandardCursorType.DragCopy);
                if ((effects & DragDropEffects.Move) != 0)
                    return _cursorFactory.GetCursorHandle(StandardCursorType.DragMove);
                if ((effects & DragDropEffects.Link) != 0)
                    return _cursorFactory.GetCursorHandle(StandardCursorType.DragLink);
                return _cursorFactory.DragNoDropCursorHandle;
            }

            return _platform.Info.DefaultCursor;
        }

        public void Dispose()
        {
            if (_pointerGrabbed)
                UngrabPointer();

            if (_originalSourceWindowInfo is { } originalSourceWindowInfo &&
                _platform.Windows.ContainsKey(_sourceWindow))
            {
                _platform.Windows[_sourceWindow] = originalSourceWindowInfo;
                _originalSourceWindowInfo = null;
            }

            _lastTarget = null;
            _currentEffects = DragDropEffects.None;

            if (_dataProvider.GetOwner() == _sourceWindow)
                _dataProvider.SetOwner(0);

            _dataProvider.Dispose();
        }

        private readonly record struct XdndTargetInfo(
            byte Version,
            IntPtr TargetWindow,
            IntPtr MessageWindow,
            X11Window? InProcessWindow);
    }
}
