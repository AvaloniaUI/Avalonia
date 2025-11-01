using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Input;
using System.Linq;
using Avalonia.Platform.Storage.FileIO;

using WindowHandle = System.IntPtr;
using System.Threading.Tasks;

namespace Avalonia.X11
{
    internal class X11DataReceiver: IDisposable
    {
        private readonly IntPtr _display;
        private readonly WindowHandle _handle;
        private readonly X11Atoms _atoms;

        private IntPtr _uriList;
        private IntPtr _textPlain;

        private bool _isIncremental = false;
         List<byte> _result = new();

        private Queue<string> _typesToLoad = new();
        private X11DataTransfer? _currentDrag;

        public X11DataReceiver(IntPtr handle, X11Info info) 
        {
            _handle = handle;
            _display = info.Display;
            _atoms = info.Atoms;

            InitializeTypeAtoms();
        }

        public bool LoadDataOnEnter(X11DataTransfer data)
        {
            if (data == null)
                return false;

            _result.Clear();
            _typesToLoad.Clear();
            _currentDrag = data;

            var supportedTypes = data.GetSupportedTypes();

            //Firstly request most usable types of data for case, if sender does not support second data requests.
            if (supportedTypes.Contains(X11DataTransfer.c_mimeFiles))
            {
                _typesToLoad.Enqueue(X11DataTransfer.c_mimeFiles);
            }

            if (supportedTypes.Contains(X11DataTransfer.c_mimeUTF8))
            {
                _typesToLoad.Enqueue(X11DataTransfer.c_mimeUTF8);
            }

            if (supportedTypes.Contains(X11DataTransfer.c_mimeUTF8_alt))
            {
                _typesToLoad.Enqueue(X11DataTransfer.c_mimeUTF8_alt);
            }

            if (supportedTypes.Contains(X11DataTransfer.c_mimeTextPlain))
            {
                _typesToLoad.Enqueue(X11DataTransfer.c_mimeTextPlain);

            }

            if(_typesToLoad.Count <= 0)
            {
                return false;
            }

            ProcessQuiery();

            return true;
        }

        public bool LoadDataOnDrop(X11DataTransfer data)
        {
            if (data == null)
                return false;

            _result.Clear();
            _typesToLoad.Clear();
            _currentDrag = data;

            var supportedTypes = data.GetSupportedTypes();
            var loadedTypes = data.GetLoadedSupportedTypes();

             if (supportedTypes.Contains(X11DataTransfer.c_mimeFiles) && !loadedTypes.Contains(X11DataTransfer.c_mimeFiles))
            {
                _typesToLoad.Enqueue(X11DataTransfer.c_mimeFiles);
            }

            if (supportedTypes.Contains(X11DataTransfer.c_mimeUTF8) && !loadedTypes.Contains(X11DataTransfer.c_mimeUTF8))
            {
                _typesToLoad.Enqueue(X11DataTransfer.c_mimeUTF8);
            }

            if (supportedTypes.Contains(X11DataTransfer.c_mimeUTF8_alt) && !loadedTypes.Contains(X11DataTransfer.c_mimeUTF8_alt))
            {
                _typesToLoad.Enqueue(X11DataTransfer.c_mimeUTF8_alt);
            }

            if (supportedTypes.Contains(X11DataTransfer.c_mimeTextPlain) && !loadedTypes.Contains(X11DataTransfer.c_mimeTextPlain))
            {
                _typesToLoad.Enqueue(X11DataTransfer.c_mimeTextPlain);

            }
                       
            
            if(loadedTypes.Count < 1)
            {
                var bestType = supportedTypes.FirstOrDefault(t => t != X11DataTransfer.c_mimeTextPlain && t != X11DataTransfer.c_mimeUTF8 && t != X11DataTransfer.c_mimeUTF8_alt && t != X11DataTransfer.c_mimeFiles);
                if (!string.IsNullOrEmpty(bestType))
                { 
                    _typesToLoad.Enqueue(bestType); 
                }                
            }

            if (_typesToLoad.Count == 0)
            {
                return false;
            }

            ProcessQuiery();

            return true;
        }

        public void RequestData(string mimeType)
        {
            var typeAtom = _atoms.GetAtom(mimeType);
            if (typeAtom == IntPtr.Zero)  return;


            // Clear any previous results
            _result.Clear();

            // Request the data
            XLib.XConvertSelection(_display, _atoms.XdndSelection, typeAtom,
                                 _atoms.XdndSelection, _handle, IntPtr.Zero);
            XLib.XFlush(_display);

            // Wait for the data with timeout
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < 500) // 500ms timeout
            {
                var ev = new XEvent();
                if (XLib.XCheckTypedWindowEvent(_display, _handle, (int)XEventName.SelectionNotify, ref ev))
                {
                    HandleSelectionNotify(ref ev.SelectionEvent, true);
                    break;
                }
                System.Threading.Thread.Sleep(10);
            }
        }

        public async Task RequestDataAsync(string mimeType)
        {
            var typeAtom = _atoms.GetAtom(mimeType);
            if (typeAtom == IntPtr.Zero)
                return;


            // Clear any previous results
            _result.Clear();

            // Request the data
            XLib.XConvertSelection(_display, _atoms.XdndSelection, typeAtom,
                                 _atoms.XdndSelection, _handle, IntPtr.Zero);
            XLib.XFlush(_display);

            // Wait for the data with timeout
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < 500) // 500ms timeout
            {
                var ev = new XEvent();
                if (XLib.XCheckTypedWindowEvent(_display, _handle, (int)XEventName.SelectionNotify, ref ev))
                {
                    HandleSelectionNotify(ref ev.SelectionEvent, true);
                    break;
                }
                await Task.Delay(10);
            }
        }


        private void ProcessQuiery()
        {
            if (_typesToLoad.Count > 0)
            {
                var type = _typesToLoad.Dequeue();
                IntPtr typeAtom = _atoms.GetAtom(type);
                if (_currentDrag !=  null && !_currentDrag.GetLoadedSupportedTypes().Contains(type) && typeAtom != IntPtr.Zero)
                {
                    XLib.XConvertSelection(_display, _atoms.XdndSelection, typeAtom,
                                         _atoms.XdndSelection, _handle, IntPtr.Zero);

                    XLib.XFlush(_display);
                }
                else
                {
                    ProcessQuiery();
                }
            }
        }

        public bool HandlePropertyEvent(ref XPropertyEvent propertyEvent)
        {
            if (!_isIncremental)
            {
                return false;
            }

            if(propertyEvent.state != (int)PropertyNotification.NewValue)
            { 
                return false; 
            }


            XLib.XGetWindowProperty(_display, _handle, propertyEvent.atom,
                                IntPtr.Zero, new IntPtr(1024), false, _atoms.AnyPropertyType,
                                out var actualType, out var actualFormat,
                                out var nitems, out var bytesAfter, out var data);

            if((ulong)nitems == 0)
            {
                //INCR transfer complete
                _isIncremental = false;

                XLib.XFree(data);
                XLib.XDeleteProperty(_display, _handle, propertyEvent.atom);

                ProcessDroppedData(actualType);

                return true;
            }

            byte[] chunk = new byte[(int)nitems];
            Marshal.Copy(data, chunk, 0, (int)nitems);
            _result.AddRange(chunk);

            XLib.XFree(data);

            //Ready for next part
            XLib.XDeleteProperty(_display, _handle, _atoms.XdndSelection);

            return true;
        }

        public void HandleSelectionNotify(ref XSelectionEvent selectionEvent, bool getCall = false)
        {
            ulong offset = 0;
            IntPtr actualType = IntPtr.Zero;

            _result.Clear();

            while (true)
            {
                var code = XLib.XGetWindowProperty(_display, _handle, _atoms.XdndSelection,
                                 (IntPtr)offset, new IntPtr(1024), false, _atoms.AnyPropertyType,
                                 out actualType, out var actualFormat,
                                 out var nitems, out var bytesAfter, out var data);

                if (code != (int)Status.Success || actualType == IntPtr.Zero)
                {
                    break;
                }

                if (actualType == _atoms.INCR)
                {
                    _isIncremental = true;
                    XLib.XFree(data);

                    /* start INCR by deleting the property */
                    XLib.XDeleteProperty(_display, _handle, _atoms.XdndSelection);

                    return;
                }

                int bytesPerItem = actualFormat / 8;
                ulong chunkBytes = (ulong)nitems * (ulong)bytesPerItem;

                byte[] chunk = new byte[chunkBytes];
                Marshal.Copy(data, chunk, 0, (int)chunkBytes);
                _result.AddRange(chunk);
                XLib.XFree(data);
                XLib.XDeleteProperty(_display, _handle, _atoms.XdndSelection);

                if ((ulong)bytesAfter == 0)
                {
                    break;
                }

                offset += chunkBytes;
            }

            ProcessDroppedData(actualType, getCall);
        }

        public event Action? DataReceived;

        private void InitializeTypeAtoms()
        {
            _uriList = _atoms.GetAtom("text/uri-list");
            _textPlain = _atoms.GetAtom("text/plain");
        }

        private void ProcessDroppedData(IntPtr dataType, bool getCall = false)
        {
            ProcessQuiery();

            if (_result.Count == 0 || dataType == IntPtr.Zero || _currentDrag == null)
            {
                _result.Clear();
                if(!getCall) DataReceived?.Invoke();
                return;
            }

            byte[] rawBytes = _result.ToArray();

            if (dataType == _uriList)
            {
                Encoding ansiEncoding = Encoding.GetEncoding(0); //system ANSI
                string data = ansiEncoding.GetString(rawBytes);
                              

                var uris = Uri.UnescapeDataString(data)
                    .Split(new[] { "\r\n", "\n" }, 
                        StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !line.StartsWith("#"))
                    .Select(line => line.StartsWith("file://") ? line.Substring(7) : line)
                    .Select(line => line.Replace("\0", "").Trim())
                    .Where(line => !string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line) && !line.Contains('\0'))
                    .Select(line => 
                        StorageProviderHelpers.TryCreateBclStorageItem(line)!)
                    .ToList();

                _currentDrag.SetData(DataFormat.File, uris);
            }
            else if (dataType == _textPlain)
            {
                Encoding ansiEncoding = Encoding.GetEncoding(0); //system ANSI
                string data = ansiEncoding.GetString(rawBytes);

                _currentDrag.SetData(DataFormat.Text, data);
            }
            else if (dataType == _atoms.UTF8_STRING)
            {
                string data = Encoding.UTF8.GetString(rawBytes);

                _currentDrag.SetData(DataFormat.Text, data);
            }  
            else
            { 
                _currentDrag.SetData(_atoms.GetAtomName(dataType) ?? string.Empty, rawBytes);
            }

            _result.Clear();
            if (!getCall)
                DataReceived?.Invoke();
        }


        public void Dispose()
        {
            _currentDrag = null;
            _result.Clear();
        }
    }
}
