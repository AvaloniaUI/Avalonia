using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input.Raw;
using Avalonia.Input;
using WindowHandle = System.IntPtr;
using System.Diagnostics;
using System.IO;
using System.Drawing;

namespace Avalonia.X11
{
    internal class X11DropTarget
    {
        private readonly X11Window _window;
        private readonly IntPtr _display;
        private readonly WindowHandle _handle;
        private readonly X11Atoms _atoms;
        private readonly X11DataReceiver _receiver;

        private IDragDropDevice? _dragDropDevice;

        private WindowHandle _dragSource;
        private X11DataObject? _currentDrag;
        private (Point, IntPtr)? _locationData;

        private bool _enterEventSent = false;
        private bool _dragInProcess = false;
        private bool _dragLeaved = false;
              
        private Action<RawInputEventArgs>? Input => _window.Input;
        private IDragDropDevice? DragDropDevice  => _dragDropDevice ??= AvaloniaLocator.Current.GetService<IDragDropDevice>();

        public X11DropTarget(X11Window window, IntPtr handle, X11Info info ) 
        { 
            _window = window;
            _handle = handle;
            _display = info.Display;
            _atoms = info.Atoms;

            _receiver = new X11DataReceiver(handle, info);
            _receiver.DataReceived += OnDataReceived;

          

            SetupXdndProtocol();
        }        

        public bool OnEvent(ref XEvent ev)
        {
            if( Input == null )
            {
                return false;
            }

            if (ev.type == XEventName.ClientMessage)
            {
                return OnClientMessageEvent(ref ev.ClientMessageEvent);
            }
            else if (ev.type == XEventName.SelectionNotify)
            {
                return OnSelectionEvent(ref ev.SelectionEvent);
            }
            else if(ev.type == XEventName.PropertyNotify)
            {
                return _receiver.HandlePropertyEvent(ref ev.PropertyEvent);
            }

            return false;
        }
               

        private void SetupXdndProtocol()
        {
            int version = 5;
            IntPtr ptr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(ptr, version);

            XLib.XChangeProperty(
                _display, _handle, _atoms.XdndAware, _atoms.XA_ATOM, 32, PropertyMode.Replace, ptr, 1);
            Marshal.FreeHGlobal(ptr);
        }

        private bool OnClientMessageEvent(ref XClientMessageEvent clientMsg) 
        {
            if(clientMsg.message_type == _atoms.XdndEnter)
            {
                HandleXdndEnter(ref clientMsg);
                return true;
            }
            else if (clientMsg.message_type == _atoms.XdndPosition)
            {
                HandleXdndPosition(ref clientMsg);
                return true;
            }
            else if (clientMsg.message_type == _atoms.XdndDrop)
            {
                HandleXdndDrop(ref clientMsg);
                return true;
            }
            else if(clientMsg.message_type == _atoms.XdndLeave)
            {
                HandleXdndLeave(ref clientMsg);
                return true;
            }

            return false;
        }      

        private void HandleXdndEnter(ref XClientMessageEvent clientMsg)
        {
            _dragSource = clientMsg.ptr1;
            IntPtr[] sourceTypes = Array.Empty<IntPtr>();

            if (((int)clientMsg.ptr2 & 1) == 0)
            {
                // Types in message
                sourceTypes = new IntPtr[3] { clientMsg.ptr3, clientMsg.ptr4, clientMsg.ptr5 };
            }
            else
            {
                // Request XdndTypeList
                sourceTypes = GetSourceTypes(_dragSource);
            }

            if(sourceTypes != null && sourceTypes.Length != 0)
            {
                var sourceTypesName = sourceTypes
                    .Where(a => a != (IntPtr)0)
                    .Select(a => _atoms.GetAtomName(a) ?? string.Empty)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToArray() ;

                if (sourceTypesName != null && sourceTypesName.Length != 0)
                {
                    _currentDrag = new X11DataObject(sourceTypesName);

                    //We do not have locations and actions here, so we will invoke DragEnter in first DragPosition
                    _enterEventSent = false;                  
                }
            }
        }

        private IntPtr[] GetSourceTypes(WindowHandle source)
        {
            IntPtr actualType;
            int actualFormat;
            IntPtr nitems, bytesAfter;
            IntPtr prop;

            int status = XLib.XGetWindowProperty(_display, source, _atoms.XdndTypeList,
                                              IntPtr.Zero, new IntPtr(1024), false, _atoms.XA_ATOM,
                                              out actualType, out actualFormat,
                                              out nitems, out bytesAfter, out prop);

            if (status != (int)Status.Success || actualType != _atoms.XA_ATOM || actualFormat != 32)
            {
                return Array.Empty<IntPtr>();
            }

            IntPtr[] types = new IntPtr[(ulong)nitems];
            unsafe
            {
                var current = (IntPtr*)prop;
                for (ulong i = 0; i < (ulong)nitems; i++)
                {
                    types[i] = (IntPtr)(current[i]);
                }
            }

            XLib.XFree(prop);
            return types;
        }


        private void HandleXdndPosition(ref XClientMessageEvent clientMsg)
        {
            if (DragDropDevice == null || _currentDrag == null)
            {
                return;
            }

            // Get coords (16:16 fixed point)
            int x = ((int)clientMsg.ptr3 >> 16) & 0xFFFF;
            int y = (int)clientMsg.ptr3 & 0xFFFF;
            var point = _window.PointToClient(new PixelPoint(x, y));
            var serverEffect = clientMsg.ptr5;
            _locationData = (point, serverEffect);

            if (_currentDrag.AllDataLoaded)
            {
                var clientEffects = RaiseDragEnterOver(point, serverEffect);

                // Send response
                SendXdndStatus(_dragSource, clientEffects, x, y);
            }
            else
            {
                _receiver.LoadData(_currentDrag);
            }           
        }

        private DragDropEffects RaiseDragEnterOver(Point point, WindowHandle serverEffect)
        {
            if (DragDropDevice == null || _currentDrag == null)
            {
                return DragDropEffects.None;
            }

            RawDragEvent args;
            if (!_enterEventSent)
            {
                args = new RawDragEvent(
                      DragDropDevice,
                      RawDragEventType.DragEnter,
                      _window.InputRoot,
                      point,
                      _currentDrag,
                      _atoms.ConvertDropEffect(serverEffect),
                      RawInputModifiers.None  //TODO: Find a way to get it from mouse events
                  );

                _enterEventSent = true;
            }
            else
            {
                args = new RawDragEvent(
                   DragDropDevice,
                   RawDragEventType.DragOver,
                   _window.InputRoot,
                   point,
                   _currentDrag,
                   _atoms.ConvertDropEffect(serverEffect),
                   RawInputModifiers.None
               );
            }

            Input?.Invoke(args);
            return args.Effects;
        }

        private void SendXdndStatus(WindowHandle source, DragDropEffects effects, int x = 0, int y = 0)
        {
            bool canAccept = effects != DragDropEffects.None;

            XClientMessageEvent response = new XClientMessageEvent
            {
                type = XEventName.ClientMessage,
                send_event = 1,
                display = _display,
                window = source,
                message_type = _atoms.XdndStatus,
                format = 32,
                ptr1 = _handle,
                ptr2 = new IntPtr(canAccept ? 1 : 0),
                //TODO: Get rectangle of target control and send it to server as "hot zone" 
                ptr3 = new IntPtr((x << 16) | y), //hot zone x and y
                ptr4 = IntPtr.Zero, //hot zone width and heights
                ptr5 = _atoms.ConvertDropEffect(effects),
            };

            XEvent xevent = new XEvent { ClientMessageEvent = response };
            XLib.XSendEvent(_display, source, false, new IntPtr((int)EventMask.NoEventMask), ref xevent);
            XLib.XFlush(_display);
        }

        private void HandleXdndDrop(ref XClientMessageEvent clientMsg)
        {
            if (DragDropDevice == null || _currentDrag == null)
            {
                SendXdndFinished(_dragSource, false);
                return;
            }

            _dragInProcess = true;

            if (_currentDrag.AllDataLoaded)
            {
                try
                {
                    RaiseInput();
                }
                finally
                {
                    SendXdndFinished(_dragSource, true);
                    _dragInProcess = false;

                    if (_dragLeaved)
                    {
                        DragLeaved();
                    }
                }
            }
            else
            {
                _receiver.LoadData(_currentDrag);
            }
        }

        private void OnDataReceived()
        {
            if (DragDropDevice == null || _currentDrag == null)
            {
                return;
            }

            if (_dragInProcess)
            {
                try
                {
                    RaiseInput();
                }
                finally
                {
                    SendXdndFinished(_dragSource, true);
                    _dragInProcess = false;

                    if (_dragLeaved)
                    {
                        DragLeaved();
                    }
                }
            }
            else
            {
                Point point = _locationData.HasValue ? _locationData.Value.Item1 : new Point(0, 0);
                IntPtr serverEffect = _locationData.HasValue ? _locationData.Value.Item2 : _atoms.XdndActionCopy;

                var clientEffects = RaiseDragEnterOver(point, serverEffect);

                var screenPoint = _window.PointToScreen(point);

                // Send response
                SendXdndStatus(_dragSource, clientEffects, screenPoint.X, screenPoint.Y);
            }
        }

        private void RaiseInput()
        {
            if (DragDropDevice == null || _currentDrag == null)
            {
                return;
            }

            Point point = _locationData.HasValue ? _locationData.Value.Item1 : new Point(0, 0);
            IntPtr serverEffect = _locationData.HasValue ? _locationData.Value.Item2 : _atoms.XdndActionCopy;


            var args = new RawDragEvent(
              DragDropDevice,
              RawDragEventType.Drop,
              _window.InputRoot,
              point,
              _currentDrag,
              _atoms.ConvertDropEffect(serverEffect),
              RawInputModifiers.None
              );

            Input?.Invoke(args);
        }

        private bool OnSelectionEvent(ref XSelectionEvent selectionEvent)
        {
            if (selectionEvent.selection == _atoms.XdndSelection)
            {
                if (selectionEvent.property == IntPtr.Zero)
                {
                    return false;
                }
                    
                if (DragDropDevice != null && _currentDrag != null)
                {
                    _receiver.HandleSelectionNotify(ref selectionEvent);                
                }
                                
                return true;                
            }

            return false;
        }       
                
        private void SendXdndFinished(WindowHandle source, bool success)
        {            
            var response = new XEvent
            {
                ClientMessageEvent =
                {
                    type = XEventName.ClientMessage,
                    message_type = _atoms.XdndFinished,
                    window = source,
                    format = 32,
                    ptr1 = _handle,
                    ptr2 = new IntPtr(success ? 1 : 0)
                }
            };
            XLib.XSendEvent(_display, source, false,
                 new IntPtr((int)(EventMask.NoEventMask)), ref response);
        }

        private void HandleXdndLeave(ref XClientMessageEvent clientMsg)
        {
            if(_dragInProcess)
            {
                _dragLeaved = true;
            }
            else
            {
                DragLeaved();
            }
        }

        private void DragLeaved()
        {
            if (DragDropDevice != null && _currentDrag != null)
            {
                Point point = _locationData.HasValue ? _locationData.Value.Item1 : new Point(0, 0);
                IntPtr serverEffect = _locationData.HasValue ? _locationData.Value.Item2 : _atoms.XdndActionCopy;

                var args = new RawDragEvent(
                    DragDropDevice,
                    RawDragEventType.DragLeave,
                     _window.InputRoot,
                      point,
                      _currentDrag,
                      _atoms.ConvertDropEffect(serverEffect),
                      RawInputModifiers.None
                      );

                Input?.Invoke(args);
            }

            _currentDrag = null;
            _locationData = null;
            _dragLeaved = false;
        }
    }
}
