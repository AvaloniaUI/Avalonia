using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Logging;
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

        using var handler = new Handler(
            platform,
            window.Handle,
            dataTransfer,
            allowedEffects,
            triggerEvent.KeyModifiers,
            cursorFactory);

        return await handler.Completion;
    }

    private sealed class Handler : IDisposable
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly IntPtr _sourceWindow;
        private readonly IDataTransfer _dataTransfer;
        private readonly DragDropEffects _allowedEffects;
        private readonly X11CursorFactory? _cursorFactory;
        private readonly DragDropDataProvider _dataProvider;
        private readonly ParametrizedLogger? _logger;
        private readonly TaskCompletionSource<DragDropEffects> _completionSource = new();
        private readonly IntPtr[] _formatAtoms;
        private X11WindowInfo? _originalSourceWindowInfo;
        private bool _pointerGrabbed;
        private DragDropEffects _currentEffects;
        private TargetState _targetState;
        private DragDropTimeoutManager? _timeoutManager;

        public Handler(
            AvaloniaX11Platform platform,
            IntPtr sourceWindow,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects,
            KeyModifiers initialKeyModifiers,
            X11CursorFactory? cursorFactory)
        {
            _platform = platform;
            _sourceWindow = sourceWindow;
            _dataTransfer = dataTransfer;
            _allowedEffects = allowedEffects;
            _cursorFactory = cursorFactory;
            _currentEffects = allowedEffects;
            _logger = Logger.TryGet(LogEventLevel.Verbose, LogArea.X11Platform);
            _dataProvider = new DragDropDataProvider(platform, dataTransfer.ToAsynchronous());

            if (!platform.Windows.TryGetValue(sourceWindow, out var sourceWindowInfo))
            {
                _formatAtoms = [];
                Complete(DragDropEffects.None);
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
                GetCursor(GetEffectiveAllowedEffects(initialKeyModifiers)),
                0);

            if (grabResult != GrabResult.GrabSuccess)
            {
                _formatAtoms = [];
                Complete(DragDropEffects.None);
                return;
            }

            _pointerGrabbed = true;

            // Replace the window event handler with our own during the drag operation.
            _originalSourceWindowInfo = sourceWindowInfo;
            _platform.Windows[_sourceWindow] = new X11WindowInfo(OnEvent, sourceWindowInfo.Window);

            var atoms = _platform.Info.Atoms;
            _formatAtoms = DataFormatHelper.ToAtoms(dataTransfer.Formats, atoms);
            _dataProvider.SetOwner(_sourceWindow);

            // Publish our formats.
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

        private DragDropTimeoutManager TimeoutManager
            => _timeoutManager ??=
                new DragDropTimeoutManager(SelectionHelper.Timeout, () => Complete(DragDropEffects.None));

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
            // Drop pending, ignore any queued position change.
            if (_targetState.PendingDrop is not null)
                return;

            var rootPosition = new PixelPoint(motion.x_root, motion.y_root);
            var target = FindXdndTarget(rootPosition);
            var modifiers = motion.state.ToRawInputModifiers();
            var effectiveAllowedEffects = GetEffectiveAllowedEffects(modifiers.ToKeyModifiers());

            // Handle new target.
            if (_targetState.Target != target)
            {
                _logger?.Log(
                    this,
                    "Pointer moved from window {OldTarget} to window {NewTarget}.",
                    _targetState.Target?.TargetWindow,
                    target?.TargetWindow);

                if (_targetState.Target is { } lastTarget)
                {
                    if (lastTarget.InProcessWindow is { } window)
                        ProcessRawDragEvent(window, RawDragEventType.DragLeave, rootPosition, modifiers);
                    else
                        SendXdndLeave(lastTarget);
                }

                _targetState = new TargetState(target);
                UpdateCurrentEffects(effectiveAllowedEffects);

                if (target is { } newTarget)
                {
                    if (newTarget.InProcessWindow is { } window)
                    {
                        var effects = ProcessRawDragEvent(window, RawDragEventType.DragEnter, rootPosition, modifiers);
                        _targetState.AllowsDrop = effects != DragDropEffects.None;
                        UpdateCurrentEffects(effects);
                    }
                    else
                        SendXdndEnter(newTarget);
                }
            }

            // Update current target.
            if (target is { } currentTarget)
            {
                if (currentTarget.InProcessWindow is { } window)
                {
                    var effects = ProcessRawDragEvent(window, RawDragEventType.DragOver, rootPosition, modifiers);
                    _targetState.AllowsDrop = effects != DragDropEffects.None;
                    UpdateCurrentEffects(effects);
                }
                else
                {
                    var action = XdndActionHelper.EffectsToAction(effectiveAllowedEffects, _platform.Info.Atoms);
                    var positionRequest = new PositionRequest(rootPosition, motion.time, action);

                    if (_targetState.IsWaitingForStatus)
                    {
                        // We already sent a position previously and are waiting for a response. Don't flood.
                        _targetState.PendingPosition = positionRequest;

                        _logger?.Log(
                            this,
                            "Pointer moved to point {Position} on window {Window} while waiting for XdndStatus. XdndPosition will be sent later.",
                            rootPosition,
                            currentTarget.TargetWindow);
                    }
                    else
                    {
                        SendPositionRequest(positionRequest, currentTarget);
                    }
                }
            }
        }

        private void OnButtonRelease(in XButtonEvent button)
        {
            UngrabPointer();

            if (_targetState.Target is not { } target || _targetState.PendingDrop is not null)
                return;

            if (target.InProcessWindow is not null)
            {
                var rootPosition = new PixelPoint(button.x_root, button.y_root);
                var modifiers = button.state.ToRawInputModifiers();

                if (_targetState.AllowsDrop)
                {
                    var effects = ProcessRawDragEvent(target.InProcessWindow, RawDragEventType.Drop, rootPosition, modifiers);
                    UpdateCurrentEffects(effects);
                }
                else
                {
                    ProcessRawDragEvent(target.InProcessWindow, RawDragEventType.DragLeave, rootPosition, modifiers);
                    UpdateCurrentEffects(DragDropEffects.None);
                }

                _targetState = default;
                Complete(_currentEffects);
            }
            else
            {
                var dropRequest = new DropRequest(button.time);

                if (_targetState.IsWaitingForStatus)
                {
                    // We're still waiting for a XdndStatus response for the last position, defer the drop.
                    _targetState.PendingDrop = dropRequest;

                    _logger?.Log(
                        this,
                        "Pointer released on window {Window} while waiting for XdndStatus. XdndDrop will be sent later.",
                        target.TargetWindow);
                }
                else if (_targetState.AllowsDrop)
                {
                    SendDropRequest(dropRequest, target);
                }
                else
                {
                    _targetState = default;
                    UpdateCurrentEffects(DragDropEffects.None);
                    SendXdndLeave(target);
                    Complete(DragDropEffects.None);
                }
            }
        }

        private void OnXdndStatus(in XClientMessageEvent message)
        {
            if (_targetState.Target is not { } target || message.ptr1 != target.TargetWindow)
                return;

            TimeoutManager.Stop();

            var accepted = (message.ptr2 & 1) == 1;
            var action = accepted ? message.ptr5 : 0;
            var effects = XdndActionHelper.ActionToEffects(action, _platform.Info.Atoms);

            _targetState.IsWaitingForStatus = false;
            _targetState.AllowsDrop = action != 0;

            UpdateCurrentEffects(effects & _allowedEffects);

            _logger?.Log(
                this,
                "Received XdndStatus with effects {Effects} for window {Window}.",
                effects,
                target.TargetWindow);

            // If we have any pending XdndPosition or XdndDrop to send, now is the time.
            if (_targetState.PendingPosition is { } position)
            {
                _targetState.PendingPosition = null;
                SendPositionRequest(position, target);
            }
            else if (_targetState.PendingDrop is { } drop)
            {
                SendDropRequest(drop, target);
            }
        }

        private void OnXdndFinished(in XClientMessageEvent message)
        {
            if (_targetState.Target is not { } target || message.ptr1 != target.TargetWindow)
                return;

            _logger?.Log(this, "Received XdndFinished for window {Window}.", target.TargetWindow);

            _targetState = default;
            _dataProvider.Activity = null;
            _timeoutManager?.Stop();

            if (target.Version >= 5)
            {
                var accepted = (message.ptr2 & 1) == 1;
                var action = accepted ? message.ptr3 : 0;
                var effects = XdndActionHelper.ActionToEffects(action, _platform.Info.Atoms);
                UpdateCurrentEffects(effects & _allowedEffects);
            }

            Complete(_currentEffects);
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

            _logger?.Log(
                this,
                "Sending XdndEnter with version {Version} to window {Window}.",
                version,
                target.TargetWindow);

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
            _logger?.Log(
                this,
                "Sending XdndPosition at point {Position} to window {Window}.",
                rootPosition,
                target.TargetWindow);

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
            _logger?.Log(this, "Sending XdndLeave to window {Window}.", target.TargetWindow);

            SendXdndMessage(_platform.Info.Atoms.XdndLeave, target.MessageWindow, 0, 0, 0, 0);
        }

        private void SendXdndDrop(XdndTargetInfo target, IntPtr timestamp)
        {
            _logger?.Log(this, "Sending XdndDrop to window {Window}.", target.TargetWindow);

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

        private void SendPositionRequest(PositionRequest request, XdndTargetInfo target)
        {
            _targetState.IsWaitingForStatus = true;
            TimeoutManager.Restart();

            SendXdndPosition(target, request.Position, request.Timestamp, request.Action);
        }

        private void SendDropRequest(DropRequest dropRequest, XdndTargetInfo target)
        {
            TimeoutManager.Restart();
            _dataProvider.Activity = TimeoutManager.Restart;

            SendXdndDrop(target, dropRequest.Timestamp);
        }

        private DragDropEffects ProcessRawDragEvent(
            X11Window targetWindow,
            RawDragEventType eventType,
            PixelPoint rootPosition,
            RawInputModifiers modifiers)
        {
            if (targetWindow.DragDropDevice is not { } dragDropDevice)
                return DragDropEffects.None;

            var localPosition = targetWindow.PointToClient(rootPosition);
            var effectiveAllowedEffects = GetEffectiveAllowedEffects(modifiers.ToKeyModifiers());

            var dragEvent = new RawDragEvent(
                dragDropDevice,
                eventType,
                targetWindow.InputRoot,
                localPosition,
                _dataTransfer,
                effectiveAllowedEffects,
                modifiers);

            dragDropDevice.ProcessRawEvent(dragEvent);

            return dragEvent.Effects & _allowedEffects;
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

        private DragDropEffects GetEffectiveAllowedEffects(KeyModifiers keyModifiers)
            => _allowedEffects & GetAllowedEffectsFromKeyModifiers(keyModifiers);

        private static DragDropEffects GetAllowedEffectsFromKeyModifiers(KeyModifiers keyModifiers)
        {
            if ((keyModifiers & KeyModifiers.Control) != 0)
                return DragDropEffects.Copy;
            if ((keyModifiers & KeyModifiers.Shift) != 0)
                return DragDropEffects.Move;
            if ((keyModifiers & KeyModifiers.Alt) != 0)
                return DragDropEffects.Link;
            return DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;
        }

        private void Complete(DragDropEffects resultEffects)
            => _completionSource.TrySetResult(resultEffects);

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

            _targetState = default;
            _currentEffects = DragDropEffects.None;

            _timeoutManager?.Dispose();

            if (_dataProvider.GetOwner() == _sourceWindow)
                _dataProvider.SetOwner(0);

            _dataProvider.Dispose();
        }

        private readonly record struct XdndTargetInfo(
            byte Version,
            IntPtr TargetWindow,
            IntPtr MessageWindow,
            X11Window? InProcessWindow);

        private readonly struct PositionRequest(PixelPoint position, IntPtr timestamp, IntPtr action)
        {
            public readonly PixelPoint Position = position;
            public readonly IntPtr Timestamp = timestamp;
            public readonly IntPtr Action = action;
        }

        private readonly struct DropRequest(IntPtr timestamp)
        {
            public readonly IntPtr Timestamp = timestamp;
        }

        private struct TargetState(XdndTargetInfo? target)
        {
            public readonly XdndTargetInfo? Target = target;
            public bool AllowsDrop;
            public bool IsWaitingForStatus; // XdndPosition sent, but no XdndStatus received yet
            public PositionRequest? PendingPosition; // Position not sent via XdndPosition yet
            public DropRequest? PendingDrop; // Drop not sent via XdndDrop yet
        }
    }
}
