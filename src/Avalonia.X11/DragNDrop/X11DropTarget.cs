using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input.Raw;
using Avalonia.Input;
using WindowHandle = System.IntPtr;
using System.Diagnostics;
using System.IO;

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

       // private StreamWriter? _streamWriter;
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
           /* IntPtr ptr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(ptr, version);

            XLib.XChangeProperty(
                _display, _handle, _atoms.XdndAware, _atoms.XA_ATOM, 32, PropertyMode.Replace, ptr, 1);
            Marshal.FreeHGlobal(ptr);*/

            XLib.XChangeProperty(_display, _handle, _atoms.XdndAware, _atoms.XA_ATOM,
                32, PropertyMode.Replace, new[] { version }, 1);
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

            //if (!File.Exists("/tmp/debugpipe2"))
            //{
            //    Process.Start("mkfifo", "/tmp/debugpipe2").WaitForExit();
            //}

            ////// Process.Start("xterm", "-e 'cat /tmp/debugpipe/' &");

            //_streamWriter = new StreamWriter("/tmp/debugpipe2");
            //_streamWriter.AutoFlush = true;
            //_streamWriter.WriteLine($"[START]{_handle}");

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

                 //   _streamWriter?.WriteLine($"\u001b[31m {DateTime.Now.Ticks}:HandleXdndEnter {_dragSource} | {string.Join("|", sourceTypesName)} \u001b[0m");

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
            
            // Отправляем ответ
            SendXdndStatus(_dragSource, args.Effects, x, y);

          //  _streamWriter?.WriteLine($"\u001b[32m {DateTime.Now.Ticks}:HandleXdndPosition {_dragSource} | {args.Effects} | {x} | {y} \u001b[0m");
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
                return;
            }

            // Select mime type to ask
            IntPtr bestType = string.IsNullOrEmpty(_currentDrag.GetBestType()) ? IntPtr.Zero : _atoms.GetAtom(_currentDrag.GetBestType());
            if (bestType == IntPtr.Zero)
            {
                SendXdndFinished(_dragSource, false);
                return;
            }

            _dragInProcess = true;

          //  _streamWriter?.WriteLine($"\u001b[33m {DateTime.Now.Ticks}:HandleXdndDrop {_dragSource} | type {_currentDrag.GetBestType()} {bestType}\u001b[0m");

            // Request data
            XLib.XConvertSelection(_display, _atoms.XdndSelection, bestType,
                                 _atoms.XdndSelection, _handle, clientMsg.ptr3);

         //   _streamWriter?.WriteLine($"\u001b[33m {DateTime.Now.Ticks}: XLib.XConvertSelection _display {_display} _atoms.XdndSelection {_atoms.XdndSelection} bestType {bestType} _atoms.XdndSelection {_atoms.XdndSelection} _handle {_handle} clientMsg.ptr3 {clientMsg.ptr3}\u001b[0m");

            XLib.XFlush(_display);
        }

        private void OnDataReceived(string type, object? data)
        {
            //  _streamWriter?.WriteLine($"\u001b[33m {DateTime.Now.Ticks}:OnDataReceived {type} | {data}\u001b[0m");

            if (DragDropDevice == null || _currentDrag == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(type))
                {
                    Point point = _locationData.HasValue ? _locationData.Value.Item1 : new Point(0, 0);
                    IntPtr serverEffect = _locationData.HasValue ? _locationData.Value.Item2 : _atoms.XdndActionCopy;

                    _currentDrag.SetData(type, data);
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
            }
            finally
            {
                SendXdndFinished(_dragSource, true);                
                _dragInProcess = false;
                
                if(_dragLeaved)
                {
                    DragLeaved();
                }
            }
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
           //         _streamWriter?.WriteLine($"\u001b[33m {DateTime.Now.Ticks}:_receiver.HandleSelectionNotify\u001b[0m");
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
