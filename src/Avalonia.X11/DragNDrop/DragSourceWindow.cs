using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform;
using SkiaSharp;

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

       /* private const int InvisibleBorder = 0;
        private const int DepthCopyFromParent = 0;
        private readonly IntPtr _visualCopyFromParent = IntPtr.Zero;
        private readonly (int X, int Y) _outOfScreen = (-1, -1);
        private readonly (int Width, int Height) _smallest = (1, 1);*/

        private readonly IDataObject _dataObject;
        private readonly IntPtr _dropEffect;
        private readonly ICursorFactory _cursorFactory;
        private readonly AvaloniaX11Platform _platform;
        private readonly IntPtr _display;
        private readonly X11Atoms _atoms;

        private readonly X11WindowFinder _x11WindowFinder;

        private IntPtr _handle;

        private IntPtr[] supportedTypes = Array.Empty<IntPtr>();

        private IntPtr targetWindow = IntPtr.Zero;
        private DragState state = DragState.Idle;
        private IntPtr _effect = IntPtr.Zero;

        private IntPtr _lastTargetWindow = IntPtr.Zero;
        private (int x, int y, IntPtr time)? _cachedPosition;
        private bool _waitingForStatus = false;


        private CancellationTokenSource finishTimeoutCts;

       // private StreamWriter _streamWriter;
       // private MemoryStream _memoryStream;


        public DragSourceWindow(AvaloniaX11Platform platform, IntPtr parent, IDataObject dataObject, IntPtr dropEffect)
        {
            _dataObject = dataObject;
            _platform = platform;
            _display = _platform.Display;
            _atoms = _platform.Info.Atoms;
            _cursorFactory = AvaloniaLocator.Current.GetRequiredService<ICursorFactory>();
            finishTimeoutCts = new CancellationTokenSource();
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


            SetupXdndProtocol();

          //  if(!File.Exists("/tmp/debugpipe"))
          //  {
          //      Process.Start("mkfifo", "/tmp/debugpipe").WaitForExit();
          //  }

            // Process.Start("xterm", "-e 'cat /tmp/debugpipe/' &");

           // _memoryStream = new MemoryStream();

          //  _streamWriter = new StreamWriter("/tmp/debugpipe");
          //  _streamWriter.AutoFlush = true;
          //  _streamWriter.WriteLine("[START]");

            
        }

        ~DragSourceWindow()
        {
            Dispose();
           // _streamWriter.WriteLine("[STOP]");
          //  _streamWriter.Close();
            

           

        }


        public bool StartDrag()
        {

            state = DragState.InProgress;
            targetWindow = IntPtr.Zero;

            if (XLib.XSetSelectionOwner(_display, _atoms.XdndSelection, _handle, IntPtr.Zero) == 0)
            {
                state = DragState.Failed;
                return false;
            }

            supportedTypes = _dataObject
                .GetDataFormats()
                .Select(X11DataObject.DataFormatToMimeFormat)
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

           // if (XLib.XGrabPointer(_display, _handle, false,
           //   EventMask.PointerMotionMask | EventMask.ButtonReleaseMask,
           //  GrabMode.GrabModeAsync, GrabMode.GrabModeAsync,
           //   IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0)
           //{
           //   state = DragState.Failed;
           //   return false;
           // }

            UpdateDragCursor();
          //  _streamWriter.WriteLine("[START DRAG]");
            return true;
        }


        private void OnEvent(ref XEvent ev)
        {
            switch (ev.type)
            {
                case XEventName.MotionNotify:
                  //  _streamWriter.WriteLine($"\u001b[32m [EVENT] {DateTime.Now.Ticks} XEventName.MotionNotify: \u001b[0m");
                    HandleMotionNotify(ref ev);
                    return;

                case XEventName.ButtonRelease:
                //    _streamWriter.WriteLine($"\u001b[32m [EVENT] {DateTime.Now.Ticks} XEventName.ButtonRelease: \u001b[0m");
                    HandleButtonRelease();
                    return;

                case XEventName.SelectionRequest:
                 //   _streamWriter.WriteLine($"\u001b[32m [EVENT] {DateTime.Now.Ticks} XEventName.SelectionRequest: \u001b[0m");
                    HandleSelectionRequest(ref ev.SelectionRequestEvent);
                    return;

                case XEventName.SelectionClear:
                 //   _streamWriter.WriteLine($"\u001b[32m [EVENT] {DateTime.Now.Ticks} XEventName.SelectionClear: \u001b[0m");
                    HandleSelectionClear(ref ev.SelectionClearEvent);
                    return;

                case XEventName.ClientMessage:
                 //   _streamWriter.WriteLine($"\u001b[32m [EVENT] {DateTime.Now.Ticks} XEventName.ClientMessage: \u001b[0m");
                    HandleClientMessage(ref ev.ClientMessageEvent);
                    return;

                case XEventName.PropertyNotify:
                 //   _streamWriter.WriteLine($"\u001b[32m [EVENT] {DateTime.Now.Ticks} XEventName.PropertyNotify: {ev.PropertyEvent.atom} \u001b[0m");
                    //HandlePropertyNotify(ref ev.PropertyEvent);
                    return;

            }
        }

        public bool OnXI2DeviceEvent(ref XIDeviceEvent ev)
        {
           // _streamWriter.WriteLine($"\u001b[32m [OnXI2DeviceEvent] {_handle} {ev.RootWindow} | {ev.EventWindow} | {ev.ChildWindow}: \u001b[0m");
            //if (ev.EventWindow != _)

            switch (ev.evtype)
            {
                case XiEventType.XI_Motion:
                    if (state != DragState.InProgress)
                        return false;

                   // _streamWriter.WriteLine("\u001b[32m [EVENT] XiEventType.XI_Motion: \u001b[0m");

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
                  //  _streamWriter.WriteLine("\u001b[32m [EVENT] XiEventType.XI_ButtonRelease: \u001b[0m");
                    HandleButtonRelease();
                    return true;
            }

            return false;
        }

        public event EventHandler<DragDropEffects>? Finished;

        private IntPtr PrepareXWindow(IntPtr display, IntPtr parent)
        {
            
           // var attrs = new XSetWindowAttributes(); 
            var handle = XLib.XCreateSimpleWindow(display, parent, 0, 0, 1, 1, 0, IntPtr.Zero, IntPtr.Zero);
            if (handle == IntPtr.Zero)
            {
                throw new Exception("Failed to create drag source window");
            }

            /*var handle = XLib.XCreateWindow(display, parent,
                _outOfScreen.X, _outOfScreen.Y,
                _smallest.Width, _smallest.Height,
                InvisibleBorder,
                DepthCopyFromParent,
                (int)CreateWindowArgs.InputOutput,
                _visualCopyFromParent,
                new UIntPtr((uint)valueMask),
                ref attrs);*/
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

          //  _streamWriter.WriteLine($"\u001b[34m [EVENT] HandleButtonRelease: {targetWindow} \u001b[0m");

            // Send any cached position before drop
            if (_cachedPosition.HasValue && targetWindow != IntPtr.Zero)
            {
                SendXdndPosition(targetWindow, _cachedPosition.Value.x, _cachedPosition.Value.y, _cachedPosition.Value.time);
                _cachedPosition = null;
                // Don't wait for status since we're about to send drop
                _waitingForStatus = false;
            }


            if (targetWindow != IntPtr.Zero)
            {
                SendXdndDrop(targetWindow);
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
               // _streamWriter.WriteLine($"\u001b[34m [EVENT] HandleClientMessage: HandleXdndStatus \u001b[0m");
                HandleXdndStatus(ref clientEvent);
            }
            else if (clientEvent.message_type == _atoms.XdndFinished)
            {
          //      _streamWriter.WriteLine($"\u001b[34m [EVENT] HandleClientMessage: HandleXdndFinished \u001b[0m");
                HandleXdndFinished(ref clientEvent);
            }
        }


        private void HandlePointerPosition(IntPtr newTarget, int x, int y, IntPtr time)
        {
            if (newTarget != targetWindow)
            {
                IntPtr proxyTarget = _x11WindowFinder.FindXdndProxy(newTarget);
                if (proxyTarget != IntPtr.Zero)
                    newTarget = proxyTarget;
            }

            if (newTarget != targetWindow)
            {
                if (newTarget != IntPtr.Zero && !_x11WindowFinder.CheckXdndSupport(newTarget))
                {
                    newTarget = _x11WindowFinder.FindXdndAwareParent(newTarget);
                }
            }

            if (newTarget != targetWindow)
            {
                // Different window under the cursor
                if (targetWindow != IntPtr.Zero)
                {
                    SendXdndLeave(targetWindow);
                }

                // Clear cache when changing windows
                _lastTargetWindow = IntPtr.Zero;
                _cachedPosition = null;
                _waitingForStatus = false;

                targetWindow = newTarget;

                
                if (targetWindow == IntPtr.Zero || !_x11WindowFinder.CheckXdndSupport(targetWindow))
                    {
                        var root = _platform.Info.RootWindow;
                        targetWindow = _x11WindowFinder.FindRealWindow(root, x, y, 6, true);
                        if (targetWindow == IntPtr.Zero)
                            targetWindow = _x11WindowFinder.FindRealWindow(root, x, y, 6, false);
                    }

            //    _streamWriter.WriteLine($"\u001b[34m [EVENT] HandlePointerPosition: {targetWindow}  ({x}, {y}) \u001b[0m");

                if (targetWindow != IntPtr.Zero)
                    {
                        SendXdndEnter(targetWindow);
                    //    _streamWriter.WriteLine($"\u001b[34m [EVENT] SendXdndPositionEnter: {targetWindow}  ({x}, {y}) \u001b[0m");
                        SendXdndPosition(targetWindow, x, y, time);
                        _waitingForStatus = true;
                        _lastTargetWindow = targetWindow;

                     }
                    else
                    {
                        HandleUnsupportedTarget();
                        return;
                    }

                
            }
            else if (targetWindow != IntPtr.Zero)
            {
                if (!_waitingForStatus)
                {

                 //   _streamWriter.WriteLine($"\u001b[34m [EVENT] SendXdndPosition: {targetWindow}  ({x}, {y}) \u001b[0m");
                    SendXdndPosition(targetWindow, x, y, time);
                }
                else
                {
                //    _streamWriter.WriteLine($"\u001b[34m [EVENT] Cache the position if we're waiting for status: {targetWindow}  ({x}, {y}) \u001b[0m");

                    // Cache the position if we're waiting for status
                    _cachedPosition = (x, y, time);
                }

            }
        }

        private void HandleUnsupportedTarget()
        {
            targetWindow = IntPtr.Zero;
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


          //  _streamWriter.WriteLine($"\u001b[37m [EVENT] SendXdndDrop: {targetWindow}  ({window}, {_handle}) \u001b[0m");


            XLib.XSendEvent(_display, window, false, IntPtr.Zero, ref evt);
            XLib.XFlush(_display);

            // Устанавливаем таймаут для ожидания ответа
            state = DragState.WaitingForFinish;
            SetupFinishTimeout();
        }

        private void SetupFinishTimeout()
        {
            var oldCts = finishTimeoutCts;
            finishTimeoutCts = new CancellationTokenSource();
            oldCts?.Cancel();
            oldCts?.Dispose();

            Task.Delay(2000, finishTimeoutCts.Token).ContinueWith(t =>
            {
                if (!t.IsCanceled && state == DragState.WaitingForFinish)
                {
                    HandleDragFailure();
                }
            }, TaskScheduler.Default);

        }

        private void HandleSelectionRequest(ref XSelectionRequestEvent requestEvent)
        {
          //  _streamWriter.WriteLine($"\u001b[31m [EVENT] {DateTime.Now.Ticks}: HandleSelectionRequest: XSelectionRequestEvent type {requestEvent.type}  serial {requestEvent.serial} send_event {requestEvent.send_event}  display {requestEvent.display} owner {requestEvent.owner}  requestor {requestEvent.requestor} selection {requestEvent.selection}  target {requestEvent.target} property {requestEvent.property}  time {requestEvent.time}\u001b[0m");


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
            //    _streamWriter.WriteLine($"\u001b[34m [EVENT] HandleSelectionRequest: {targetWindow}  ({requestEvent.requestor}) _atoms.TARGETS\u001b[0m");

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
            else if(supportedTypes.Contains(requestEvent.target))
            {
            //    _streamWriter.WriteLine($"\u001b[34m [EVENT] HandleSelectionRequest: {targetWindow}  ({requestEvent.requestor}) data {requestEvent.target}\u001b[0m");

                byte[] dataBytes = X11DataObject.ToTransfer(_dataObject,
                    X11DataObject.MimeFormatToDataFormat(_atoms.GetAtomName(requestEvent.target) ?? string.Empty));


                // Устанавливаем свойство с данными
                XLib.XChangeProperty(
                    _display,
                    requestEvent.requestor,
                    requestEvent.property,
                    requestEvent.target,
                    8, 
                    PropertyMode.Replace,
                    dataBytes,
                    dataBytes.Length
                );

                // Указываем в ответе, что данные предоставлены
                responseEvent.property = requestEvent.property;

            }
            else
            {
          //      _streamWriter.WriteLine($"\u001b[31m [EVENT] HandleSelectionRequest: {targetWindow}  ({requestEvent.requestor}) unsupported {requestEvent.target}\u001b[0m");

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

          //  _streamWriter.WriteLine($"\u001b[35m [HandleXdndStatus] {statusEvent.window}: {accepted} \u001b[0m");
            if (!accepted)
            {
                HandleDragFailure();
            }

            _effect = statusEvent.ptr5;
            UpdateDragCursor();

            // We've received status, can send cached position if available
            _waitingForStatus = false;
            if (_cachedPosition.HasValue && targetWindow != IntPtr.Zero)
            {
                SendXdndPosition(targetWindow, _cachedPosition.Value.x, _cachedPosition.Value.y, _cachedPosition.Value.time);
                _cachedPosition = null;
                _waitingForStatus = true; // Wait for next status
            }
        }

        private void HandleXdndFinished(ref XClientMessageEvent finishedEvent)
        {
            if (state == DragState.WaitingForFinish)
            {
                bool success = ((ulong)finishedEvent.ptr2 & 1) != 0;
            //    _streamWriter.WriteLine($"\u001b[35m [HandleXdndFinished] {finishedEvent.window}: {success} \u001b[0m");
                if (success)
                {
                    state = DragState.Completed;
                    CleanupAfterDrag();
                    DragDropEffects result = _atoms.ConvertDropEffect(_effect);
                    if (result == DragDropEffects.None)
                        result = DragDropEffects.Copy;

                  //  using (StreamWriter streamWriter = new StreamWriter("/tmp/debugpipe"))
                   // {
                  //      _memoryStream.Position = 0;
                  //      _memoryStream.CopyTo(streamWriter.BaseStream);
                   // }

                    Finished?.Invoke(this, result);
                }
                else
                {
                    HandleDragFailure(); //Target reported drop failure
                }                   
            }
        }

        private void HandleDragFailure()
        {
         //   _streamWriter.WriteLine($"\u001b[35m [HandleDragFailure]  \u001b[0m");
            if (targetWindow != IntPtr.Zero)
            {
                SendXdndLeave(targetWindow);
            }

            CleanupAfterDrag();
            state = DragState.Failed;

          //  using (StreamWriter streamWriter = new StreamWriter("/tmp/debugpipe"))
          //  {
          //      _memoryStream.Position = 0;
          //      _memoryStream.CopyTo(streamWriter.BaseStream);
          //  }

            Finished?.Invoke(this, DragDropEffects.None);
        }

        private void CancelDragOperation()
        {
         //   _streamWriter.WriteLine($"\u001b[35m [CancelDragOperation]  \u001b[0m");

            if (state != DragState.InProgress)
                return;

            if (targetWindow != IntPtr.Zero)
            {
                SendXdndLeave(targetWindow);
            }

            CleanupAfterDrag();
            state = DragState.Cancelled;

            Finished?.Invoke(this, DragDropEffects.None);
        }

        private void CleanupAfterDrag()
        {
            finishTimeoutCts?.Cancel();
            XLib.XUngrabPointer(_display, IntPtr.Zero);
            SetCursor(null); 
        }

        private void UpdateDragCursor()
        {
            ICursorImpl? cursorShape = null;

            switch (state)
            {
                case DragState.InProgress:
                    if (targetWindow == IntPtr.Zero)
                    {
                        cursorShape = _cursorFactory.GetCursor(StandardCursorType.Hand);
                    }
                    else if (((uint)_dropEffect & (uint)(_atoms.XdndActionCopy)) == (uint)_atoms.XdndActionCopy)
                    {
                        cursorShape = _cursorFactory.GetCursor(StandardCursorType.DragCopy);
                    }
                    else if (((uint)_dropEffect & (uint)(_atoms.XdndActionMove)) == (uint)_atoms.XdndActionMove)
                    {
                        cursorShape = _cursorFactory.GetCursor(StandardCursorType.DragMove);
                    }
                    else if (((uint)_dropEffect & (uint)(_atoms.XdndActionLink)) == (uint)_atoms.XdndActionLink)
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

        public void SetCursor(ICursorImpl? cursor)
        {
            if (cursor == null)
                XLib.XDefineCursor(_display, _handle, _platform.Info.DefaultCursor);
            else if (cursor is CursorImpl impl)
            {
                XLib.XDefineCursor(_display, _handle, impl.Handle);
            }
        }

        public void Dispose()
        {
            finishTimeoutCts?.Cancel();
            finishTimeoutCts?.Dispose();

            if (_handle != IntPtr.Zero)
            {
                _platform.Windows.Remove(_handle);
                _platform.XI2?.OnWindowDestroyed(_handle);
                _handle = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);

        }       
    }
    
}
