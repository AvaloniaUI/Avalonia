using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.X11.DragNDrop;

namespace Avalonia.X11
{
    internal class DragSourceWindow : IDisposable
    {
        private enum DragState
        {
            Idle,
            InProgress,
            WaitingForFinish,
            Completed,
            Cancelled,
            Failed
        }

        private const int InvisibleBorder = 0;
        private readonly (int X, int Y) _outOfScreen = (0, 0);
        private readonly (int Width, int Height) _smallest = (1, 1);

        private readonly IDataTransfer _dataTransfer;
        private readonly IntPtr _dropEffect;
        private readonly ICursorFactory _cursorFactory;
        private readonly AvaloniaX11Platform _platform;
        private readonly IntPtr _display;
        private readonly X11Atoms _atoms;

        private readonly X11WindowFinder _x11WindowFinder;
        private readonly X11DataTransmitter _dataTransmitter;

        private IntPtr _handle;

        private IntPtr[] supportedTypes = Array.Empty<IntPtr>();

        private IntPtr _targetWindow = IntPtr.Zero;
        private DragState state = DragState.Idle;
        private IntPtr _effect = IntPtr.Zero;
        private bool _sameAppTargetWindow = false;
        private Ix11InnerDropTarget? _innerTarget = null;

        private IntPtr _lastTargetWindow = IntPtr.Zero;
        private (int x, int y, IntPtr time)? _cachedPosition;
        private (int x, int y)? _lastPosition;
        private bool _waitingForStatus = false;
        private bool _isFinished = false;

        private CancellationTokenSource _finishTimeoutCts;

        private DispatcherTimer? _sameAppDragOverTimer;
        private (IntPtr window, int x, int y)? _pendingSameAppDragOver;

        public DragSourceWindow(AvaloniaX11Platform platform, IntPtr parent, IDataTransfer dataTransfer, IntPtr dropEffect)
        {
            _dataTransfer = dataTransfer;
            _platform = platform;
            _display = _platform.Display;
            _atoms = _platform.Info.Atoms;
            _cursorFactory = AvaloniaLocator.Current.GetRequiredService<ICursorFactory>();
            _finishTimeoutCts = new CancellationTokenSource();
            _dropEffect = dropEffect;

            _x11WindowFinder = new X11WindowFinder(_display, _atoms);


            _handle = PrepareXWindow(platform.Info.Display, parent);
            _platform.Windows[_handle] = OnEvent;
            var valueMask = EventMask.PropertyChangeMask;

            if (platform.XI2 == null)
            {
                valueMask |= EventMask.PointerMotionMask
                            | EventMask.ButtonPressMask
                            | EventMask.ButtonReleaseMask
                            | EventMask.ExposureMask;
            }

            XLib.XSelectInput(_display, _handle, new IntPtr((uint)valueMask));

            _dataTransmitter = new X11DataTransmitter(_handle, _platform.Info);

            SetupXdndProtocol();
        }
               
        public bool StartDrag()
        {
            state = DragState.InProgress;
            _targetWindow = IntPtr.Zero;

            if (XLib.XSetSelectionOwner(_display, _atoms.XdndSelection, _handle, IntPtr.Zero) == 0)
            {
                state = DragState.Failed;
                return false;
            }

            supportedTypes = _dataTransfer.Formats
                .Select(X11DataTransfer.DataFormatToMimeFormat)
                .Select(_atoms.GetAtom)
                .Where(ptr => ptr != IntPtr.Zero)
                .ToArray();

            if (supportedTypes.Length <= 0)
            {
                return false;
            }

            byte[] typesBytes = new byte[supportedTypes.Length * 4];
            for (int i = 0; i < supportedTypes.Length; i++)
            {
                byte[] typeBytes = BitConverter.GetBytes(supportedTypes[i].ToInt32());
                Array.Copy(typeBytes, 0, typesBytes, i * 4, 4);
            }
            XLib.XChangeProperty(_display, _handle,
                _atoms.XdndTypeList, _atoms.XA_ATOM, 32, PropertyMode.Replace,
                typesBytes, supportedTypes.Length);

            XLib.XSetInputFocus(_display, _handle, RevertTo.Parent, IntPtr.Zero);

            UpdateDragCursor(_dropEffect);
            return true;
        }


        private void OnEvent(ref XEvent ev)
        {
            switch (ev.type)
            {
                case XEventName.MotionNotify:
                    HandleMotionNotify(ref ev);
                    return;

                case XEventName.ButtonRelease:
                    HandleButtonRelease();
                    return;

                case XEventName.SelectionRequest:
                    HandleSelectionRequest(ref ev.SelectionRequestEvent);
                    return;

                case XEventName.SelectionClear:
                    HandleSelectionClear(ref ev.SelectionClearEvent);
                    return;

                case XEventName.ClientMessage:
                    HandleClientMessage(ref ev.ClientMessageEvent);
                    return;

                case XEventName.PropertyNotify:
                    _dataTransmitter.HandlePropertyNotify(ref ev.PropertyEvent);
                    return;
            }
        }

        public bool OnXI2DeviceEvent(ref XIDeviceEvent ev)
        {
            switch (ev.evtype)
            {
                case XiEventType.XI_Motion:
                    if (state != DragState.InProgress)
                        return false;

                    IntPtr root = XLib.XDefaultRootWindow(_display);

                    IntPtr targetWindow = _x11WindowFinder.FindTopWindowUnderCursor(
                         root,
                         (int)ev.root_x,
                         (int)ev.root_y
                        );

                    if (targetWindow != IntPtr.Zero)
                    {
                        HandlePointerPosition(targetWindow, (int)ev.root_x, (int)ev.root_y, ev.time);
                    }

                    return true;

                case XiEventType.XI_ButtonRelease:
                    HandleButtonRelease();
                    return true;
            }

            return false;
        }

        public event EventHandler<DragDropEffects>? Finished;

        private IntPtr PrepareXWindow(IntPtr display, IntPtr parent)
        {
            var handle = XLib.XCreateSimpleWindow(display, parent,
                _outOfScreen.X, _outOfScreen.Y,
                _smallest.Width, _smallest.Height,
                InvisibleBorder,
                IntPtr.Zero, IntPtr.Zero);

            if (handle == IntPtr.Zero)
            {
                throw new Exception("Failed to create drag source window");
            }

            XLib.XMapWindow(display, handle);
            return handle;
        }

        private void SetupXdndProtocol()
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                int version = 5;
                ptr = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(ptr, version);

                XLib.XChangeProperty(_display, _handle, _atoms.XdndAware, _atoms.XA_ATOM, 32, PropertyMode.Replace, ptr, 1);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        private void HandleMotionNotify(ref XEvent ev)
        {
            if (state != DragState.InProgress)
                return;

            IntPtr target = _x11WindowFinder.FindTopWindowUnderCursor(_platform.Info.DefaultRootWindow, out var rootX, out var rootY);
            if (target != IntPtr.Zero)
            {
                HandlePointerPosition(target, rootX, rootY, ev.MotionEvent.time);
            }
        }

        private void HandleButtonRelease()
        {
            if (state != DragState.InProgress)
                return;

            // Send any cached position before drop
            if (_cachedPosition.HasValue && _targetWindow != IntPtr.Zero)
            {
                SendXdndPosition(_targetWindow, _cachedPosition.Value.x, _cachedPosition.Value.y, _cachedPosition.Value.time);
                _cachedPosition = null;
                // Don't wait for status since we're about to send drop
                _waitingForStatus = false;
            }

            if (_targetWindow != IntPtr.Zero)
            {
                if (_sameAppTargetWindow)
                {
                    SendSameAppDrop(_targetWindow);
                }
                else
                {
                    SendXdndDrop(_targetWindow);
                }
            }
            else
            {
                CancelDragOperation();
            }
        }

        private void HandleSelectionClear(ref XSelectionClearEvent clearEvent)
        {
            if (state == DragState.InProgress || state == DragState.WaitingForFinish)
            {
                HandleDragFailure();
            }
        }

        private void HandleClientMessage(ref XClientMessageEvent clientEvent)
        {
            if (clientEvent.message_type == _atoms.XdndStatus)
            {
                HandleXdndStatus(ref clientEvent);
            }
            else if (clientEvent.message_type == _atoms.XdndFinished)
            {
                HandleXdndFinished(ref clientEvent);
            }
        }

        private void HandlePointerPosition(IntPtr newTarget, int x, int y, IntPtr time)
        {
            if (state != DragState.InProgress)
                return;

            if (newTarget != _targetWindow)
            {
                IntPtr proxyTarget = _x11WindowFinder.FindXdndProxy(newTarget);
                if (proxyTarget != IntPtr.Zero)
                    newTarget = proxyTarget;
            }

            if (newTarget != _targetWindow)
            {
                if (newTarget != IntPtr.Zero && !_x11WindowFinder.CheckXdndSupport(newTarget))
                {
                    newTarget = _x11WindowFinder.FindXdndAwareParent(newTarget);
                }
            }

            if (newTarget != _targetWindow)
            {
                // Different window under the cursor
                if (_targetWindow != IntPtr.Zero)
                {

                    if (_sameAppTargetWindow)
                    {
                        SendSameAppDragLeave(_targetWindow, x, y);
                    }
                    else
                    {
                        SendXdndLeave(_targetWindow);
                    }
                }

                // Clear cache when changing windows
                _lastTargetWindow = IntPtr.Zero;
                _cachedPosition = null;
                _waitingForStatus = false;
                _innerTarget = null;

                _targetWindow = newTarget;
                _sameAppTargetWindow = false;
                _sameAppDragOverTimer?.Stop();
                _sameAppDragOverTimer = null;
                _pendingSameAppDragOver = null;

                if (_targetWindow == IntPtr.Zero || !_x11WindowFinder.CheckXdndSupport(_targetWindow))
                {
                    var root = _platform.Info.RootWindow;
                    _targetWindow = _x11WindowFinder.FindRealWindow(root, x, y, 6, true);
                    if (_targetWindow == IntPtr.Zero)
                    {
                        _targetWindow = _x11WindowFinder.FindRealWindow(root, x, y, 6, false);
                    }
                }

                if (_targetWindow != IntPtr.Zero)
                {
                    if (IsSameAppWindow(_targetWindow))
                    {
                        _sameAppTargetWindow = true;
                        SendSameAppDragEnter(_targetWindow, x, y);
                        _lastPosition = (x, y);
                    }
                    else
                    {
                        SendXdndEnter(_targetWindow);
                        SendXdndPosition(_targetWindow, x, y, time);
                        _waitingForStatus = true;
                    }


                    _lastTargetWindow = _targetWindow;
                }
                else
                {
                    HandleUnsupportedTarget();
                    return;
                }
            }
            else if (_targetWindow != IntPtr.Zero)
            {
                if (!_waitingForStatus)
                {
                    if (_sameAppTargetWindow)
                    {
                        if (_lastPosition == null ||
                            Math.Abs(_lastPosition.Value.x - x) > 5 ||
                            Math.Abs(_lastPosition.Value.y - y) > 5)
                        {

                            ScheduleSameAppDragOver(_targetWindow, x, y);
                            _lastPosition = (x, y);
                        }
                        else
                        {
                            _cachedPosition = (x, y, time);
                        }
                    }
                    else
                    {
                        SendXdndPosition(_targetWindow, x, y, time);
                        _waitingForStatus = true;
                    }


                }
                else
                {
                    // Cache the position if we're waiting for status
                    _cachedPosition = (x, y, time);
                }
            }
        }

        private void HandleUnsupportedTarget()
        {
            _targetWindow = IntPtr.Zero;
            SetCursor(_cursorFactory.GetCursor(StandardCursorType.Cross));
        }

        private void SendXdndEnter(IntPtr window)
        {
            XEvent evt = new XEvent
            {
                ClientMessageEvent = new XClientMessageEvent
                {
                    type = XEventName.ClientMessage,
                    display = _display,
                    window = window,
                    message_type = _atoms.XdndEnter,
                    format = 32,
                    ptr1 = _handle,
                    ptr2 = new IntPtr(5), //  XDND version
                    ptr3 = IntPtr.Zero,
                    ptr4 = IntPtr.Zero,
                    ptr5 = IntPtr.Zero
                }
            };

            XLib.XSendEvent(_display, window, false, IntPtr.Zero, ref evt);
            XLib.XFlush(_display);
        }

        private void SendXdndPosition(IntPtr window, int x, int y, IntPtr time)
        {
            XEvent evt = new XEvent
            {
                ClientMessageEvent = new XClientMessageEvent
                {
                    type = XEventName.ClientMessage,
                    display = _display,
                    window = window,
                    message_type = _atoms.XdndPosition,
                    format = 32,
                    ptr1 = _handle,
                    ptr2 = IntPtr.Zero, // flags
                    ptr3 = new IntPtr((x << 16) | y), // location
                    ptr4 = time,// time 
                    ptr5 = _dropEffect //action
                }
            };

            XLib.XSendEvent(_display, window, false, IntPtr.Zero, ref evt);
            XLib.XFlush(_display);
        }

        private void SendXdndLeave(IntPtr window)
        {
            XEvent evt = new XEvent
            {
                ClientMessageEvent = new XClientMessageEvent
                {
                    type = XEventName.ClientMessage,
                    display = _display,
                    window = window,
                    message_type = _atoms.XdndLeave,
                    format = 32,
                    ptr1 = _handle,
                    ptr2 = IntPtr.Zero,
                    ptr3 = IntPtr.Zero,
                    ptr4 = IntPtr.Zero,
                    ptr5 = IntPtr.Zero
                }
            };

            XLib.XSendEvent(_display, window, false, IntPtr.Zero, ref evt);
            XLib.XFlush(_display);
        }

        private void SendXdndDrop(IntPtr window)
        {
            XEvent evt = new XEvent
            {
                ClientMessageEvent = new XClientMessageEvent
                {
                    type = XEventName.ClientMessage,
                    display = _display,
                    window = window,
                    message_type = _atoms.XdndDrop,
                    format = 32,
                    ptr1 = _handle,
                    ptr2 = IntPtr.Zero, // flags
                    ptr3 = IntPtr.Zero, // time
                    ptr4 = IntPtr.Zero,
                    ptr5 = IntPtr.Zero
                }
            };

            XLib.XSendEvent(_display, window, false, IntPtr.Zero, ref evt);
            XLib.XFlush(_display);

            state = DragState.WaitingForFinish;
            SetupFinishTimeout(3500);
        }

        private void SetupFinishTimeout(int delay)
        {
            var oldCts = _finishTimeoutCts;
            _finishTimeoutCts = new CancellationTokenSource();
            oldCts?.Cancel();
            oldCts?.Dispose();

            var currentCts = _finishTimeoutCts;
            Task.Delay(delay, currentCts.Token).ContinueWith(t =>
            {
                if (!t.IsCanceled && !currentCts.Token.IsCancellationRequested && state == DragState.WaitingForFinish)
                {
                    HandleDragFailure();
                }
            }, TaskScheduler.Default);
        }

        private void HandleSelectionRequest(ref XSelectionRequestEvent requestEvent)
        {
            if (requestEvent.selection != _atoms.XdndSelection || requestEvent.owner != _handle)
            {
                return;
            }

            XSelectionEvent responseEvent = new XSelectionEvent
            {
                type = XEventName.SelectionNotify,
                display = requestEvent.display,
                requestor = requestEvent.requestor,
                selection = requestEvent.selection,
                target = requestEvent.target,
                property = IntPtr.Zero,
                time = requestEvent.time
            };

            if (requestEvent.target == _atoms.TARGETS)
            {
                byte[] typesBytes = new byte[supportedTypes.Length * 4];
                for (int i = 0; i < supportedTypes.Length; i++)
                {
                    byte[] typeBytes = BitConverter.GetBytes(supportedTypes[i].ToInt32());
                    Array.Copy(typeBytes, 0, typesBytes, i * 4, 4);
                }

                XLib.XChangeProperty(_display, requestEvent.requestor,
                    requestEvent.property, _atoms.XA_ATOM, 32, PropertyMode.Replace,
                    typesBytes, supportedTypes.Length);

                responseEvent.property = requestEvent.property;
            }
            else if (supportedTypes.Contains(requestEvent.target))
            {
                _dataTransmitter.StartTransfer(ref requestEvent, _dataTransfer);

                // indicate in the response that the data has been provided
                responseEvent.property = requestEvent.property;
            }

            XEvent evt = new XEvent
            {
                SelectionEvent = responseEvent,
            };

            XLib.XSendEvent(_display, requestEvent.requestor, false, IntPtr.Zero, ref evt);
            XLib.XFlush(_display);
        }

        private void HandleXdndStatus(ref XClientMessageEvent statusEvent)
        {
            if (state != DragState.InProgress && state != DragState.WaitingForFinish)
                return;

            bool accepted = ((ulong)statusEvent.ptr2 & 1) != 0;

            if (!accepted)
            {
                HandleDragFailure();
            }

            _effect = statusEvent.ptr5;
            UpdateDragCursor(_effect);

            // We've received status, can send cached position if available
            _waitingForStatus = false;
            if (_cachedPosition.HasValue && _targetWindow != IntPtr.Zero)
            {
                SendXdndPosition(_targetWindow, _cachedPosition.Value.x, _cachedPosition.Value.y, _cachedPosition.Value.time);
                _cachedPosition = null;
                _waitingForStatus = true; // Wait for next status
            }
        }

        private void HandleXdndFinished(ref XClientMessageEvent finishedEvent)
        {
            if (_handle == IntPtr.Zero)
                return;

            if (state == DragState.WaitingForFinish)
            {
                bool success = ((ulong)finishedEvent.ptr2 & 1) != 0;

                if (success)
                {
                    state = DragState.Completed;
                    CleanupAfterDrag();
                    DragDropEffects result = _atoms.ConvertDropEffect(_effect);
                    if (result == DragDropEffects.None)
                        result = DragDropEffects.Copy;

                    InvokeFinished(result);
                }
                else
                {
                    HandleDragFailure(); //Target reported drop failure
                }
            }
        }

        private void HandleDragFailure()
        {
            if (_handle == IntPtr.Zero)
                return;

            if (_targetWindow != IntPtr.Zero)
            {
                SendXdndLeave(_targetWindow);
            }

            CleanupAfterDrag();
            state = DragState.Failed;

            InvokeFinished(DragDropEffects.None);
        }

        private void CancelDragOperation()
        {
            if (state != DragState.InProgress)
                return;

            if (_targetWindow != IntPtr.Zero)
            {
                SendXdndLeave(_targetWindow);
            }

            CleanupAfterDrag();
            state = DragState.Cancelled;

            InvokeFinished(DragDropEffects.None);
        }

        private void CleanupAfterDrag()
        {
            if (_finishTimeoutCts != null && _finishTimeoutCts.Token.CanBeCanceled)
            {
                _finishTimeoutCts?.Cancel();
            }

            XLib.XUngrabPointer(_display, IntPtr.Zero);
            XLib.XUnmapWindow(_display, _handle);
            XLib.XFlush(_display);

            SetCursor(null);
            _innerTarget = null;
        }

        private void UpdateDragCursor(IntPtr effect)
        {
            ICursorImpl? cursorShape = null;

            switch (state)
            {
                case DragState.InProgress:
                    if (_targetWindow == IntPtr.Zero)
                    {
                        cursorShape = _cursorFactory.GetCursor(StandardCursorType.Hand);
                    }
                    else if (((uint)effect & (uint)(_atoms.XdndActionCopy)) == (uint)_atoms.XdndActionCopy)
                    {
                        cursorShape = _cursorFactory.GetCursor(StandardCursorType.DragCopy);
                    }
                    else if (((uint)effect & (uint)(_atoms.XdndActionMove)) == (uint)_atoms.XdndActionMove)
                    {
                        cursorShape = _cursorFactory.GetCursor(StandardCursorType.DragMove);
                    }
                    else if (((uint)effect & (uint)(_atoms.XdndActionLink)) == (uint)_atoms.XdndActionLink)
                    {
                        cursorShape = _cursorFactory.GetCursor(StandardCursorType.DragLink);
                    }
                    else
                    {
                        cursorShape = _cursorFactory.GetCursor(StandardCursorType.Cross);
                    }

                    break;

                case DragState.Failed:
                case DragState.Cancelled:
                    cursorShape = _cursorFactory.GetCursor(StandardCursorType.Cross);
                    break;
            }

            SetCursor(cursorShape);
        }

        private bool IsSameAppWindow(IntPtr targetWindow)
        {
            return _platform.IsAppWindow(targetWindow);
        }

        private void SendSameAppDragEnter(IntPtr window, int x, int y)
        {
            _innerTarget = _platform.GetDropTarget(window);

            if (_innerTarget != null)
            {
                var point = new PixelPoint(x, y);
                var result = _atoms.ConvertDropEffect(
                    _innerTarget.HandleDragEnter(point, _dataTransfer, _atoms.ConvertDropEffect(_dropEffect)));

                _waitingForStatus = false;
                _effect = result;
                UpdateDragCursor(result);
            }
            else
            {
                _waitingForStatus = false;
            }
        }

        private void ScheduleSameAppDragOver(IntPtr window, int x, int y)
        {
            _sameAppDragOverTimer?.Stop();
            _pendingSameAppDragOver = (window, x, y);

            _sameAppDragOverTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(32)
            };

            _sameAppDragOverTimer.Tick -= OneSameAppDragOverTimerTick;
            _sameAppDragOverTimer.Tick += OneSameAppDragOverTimerTick;
            _sameAppDragOverTimer.Start();
        }

        private void OneSameAppDragOverTimerTick(object? sender, EventArgs e)
        {
            _sameAppDragOverTimer?.Stop();

            if (_pendingSameAppDragOver.HasValue && state == DragState.InProgress)
            {
                var (window, x, y) = _pendingSameAppDragOver.Value;

                if (window == _targetWindow && _sameAppTargetWindow)
                {
                    SendSameAppDragOver(window, x, y);
                }
            }
        }

        private void SendSameAppDragOver(IntPtr window, int x, int y)
        {
            _pendingSameAppDragOver = null;

            if (_innerTarget == null)
            {
                _innerTarget = _platform.GetDropTarget(window);
            }

            if (_innerTarget != null)
            {
                Dispatcher.UIThread.Send(_ =>
                {
                    var point = new PixelPoint(x, y);
                    var result = _atoms.ConvertDropEffect(
                         _innerTarget.HandleDragOver(point, _atoms.ConvertDropEffect(_dropEffect)));

                    _waitingForStatus = false;
                    _effect = result;
                    UpdateDragCursor(result);
                });
            }
        }

        private void SendSameAppDragLeave(IntPtr window, int x, int y)
        {
            if (_innerTarget == null)
            {
                _innerTarget = _platform.GetDropTarget(window);
            }

            if (_innerTarget != null)
            {
                Task.Run(async () =>
                {
                    var point = new PixelPoint(x, y);
                    await _innerTarget.HandleDragLeave(point, _atoms.ConvertDropEffect(_dropEffect));
                }).ConfigureAwait(false);
            }

            _waitingForStatus = false;
            _innerTarget = null;
        }

        private void SendSameAppDrop(IntPtr window)
        {
            if (_innerTarget == null)
            {
                _innerTarget = _platform.GetDropTarget(window);
            }

            if (_innerTarget != null)
            {
                state = DragState.WaitingForFinish;
                SetupFinishTimeout(5500);

                Dispatcher.UIThread.Send(_ =>
                {
                    try
                    {
                        if (state != DragState.InProgress && state != DragState.WaitingForFinish)
                        {
                            return;
                        }

                        if (_pendingSameAppDragOver.HasValue)
                        {
                            _sameAppDragOverTimer?.Stop();
                            _sameAppDragOverTimer = null;
                            var point = new PixelPoint(_pendingSameAppDragOver.Value.x, _pendingSameAppDragOver.Value.y);
                            _pendingSameAppDragOver = null;
                            _effect = _atoms.ConvertDropEffect(
                                        _innerTarget.HandleDragOver(point, _atoms.ConvertDropEffect(_dropEffect)));

                            UpdateDragCursor(_effect);
                        }
                        DragDropEffects result = _innerTarget.HandleDrop(_atoms.ConvertDropEffect(_dropEffect));

                        
                        if (result == DragDropEffects.None)
                            result = DragDropEffects.Copy;

                        _effect = _atoms.ConvertDropEffect(result);
                        UpdateDragCursor(_effect);

                        state = DragState.Completed;
                        CleanupAfterDrag();
                        InvokeFinished(result);

                    }
                    catch (Exception ex)
                    {
                        if (state != DragState.Completed)
                            HandleDragFailure();

                    }
                    finally
                    {
                        _innerTarget = null;
                        _waitingForStatus = false;
                    }
                });
            }
            else
            {
                HandleDragFailure();
            }
        }

        public void SetCursor(ICursorImpl? cursor)
        {
            if (cursor == null)
                XLib.XDefineCursor(_display, _handle, _platform.Info.DefaultCursor);
            else if (cursor is CursorImpl impl)
            {
                XLib.XDefineCursor(_display, _handle, impl.Handle);
            }
        }

        private void InvokeFinished(DragDropEffects result)
        {
            if (!_isFinished)
            {
                _isFinished = true;
                Finished?.Invoke(this, result);
            }
        }

        public void Dispose()
        {
            _sameAppDragOverTimer?.Stop();
            _sameAppDragOverTimer = null;
            _pendingSameAppDragOver = null;

            if (state != DragState.Completed)
            {
                state = DragState.Cancelled;
            }

            try
            {
                if (_finishTimeoutCts != null && _finishTimeoutCts.Token.CanBeCanceled)
                {
                    _finishTimeoutCts?.Cancel();
                }
                _finishTimeoutCts?.Dispose();
            }
            catch (ObjectDisposedException) { }

            if (_handle != IntPtr.Zero)
            {
                _platform.Windows.Remove(_handle);
                _platform.XI2?.OnWindowDestroyed(_handle);

                XLib.XUngrabPointer(_display, IntPtr.Zero);
                XLib.XUnmapWindow(_display, _handle);
                XLib.XDestroyWindow(_display, _handle);
                XLib.XFlush(_display);

                _handle = IntPtr.Zero;
            }

            _dataTransmitter.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
