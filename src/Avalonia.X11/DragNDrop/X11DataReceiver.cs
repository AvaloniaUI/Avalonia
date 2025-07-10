using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Input;
using System.Linq;
using Avalonia.Platform.Storage.FileIO;

using WindowHandle = System.IntPtr;

namespace Avalonia.X11
{
    internal class X11DataReceiver
    {
        private readonly IntPtr _display;
        private readonly WindowHandle _handle;
        private readonly X11Atoms _atoms;

        private IntPtr _uriList;
        private IntPtr _textPlain;

        private bool _isIncremental = false;
        List<byte> _result = new List<byte>();

        public X11DataReceiver(IntPtr handle, X11Info info) 
        {
            _handle = handle;
            _display = info.Display;
            _atoms = info.Atoms;

            InitializeTypeAtoms();
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

        public void HandleSelectionNotify(ref XSelectionEvent selectionEvent)
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

            ProcessDroppedData(actualType);
        }

        public event Action<string, object?>? DataReceived;

        private void InitializeTypeAtoms()
        {
            _uriList = _atoms.GetAtom("text/uri-list");
            _textPlain = _atoms.GetAtom("text/plain");
        }

        private void ProcessDroppedData(IntPtr dataType)
        {
            if (_result.Count == 0 || dataType == IntPtr.Zero)
            {
                DataReceived?.Invoke(string.Empty, null);
                _result.Clear();
                return;
            }

            if (dataType == _uriList)
            {
                Encoding ansiEncoding = Encoding.GetEncoding(0); //system ANSI
                string data = ansiEncoding.GetString(_result.ToArray());
                              

                var uris = Uri.UnescapeDataString(data)
                    .Split(new[] { "\r\n", "\n" }, 
                        StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.StartsWith("file://"))
                    .Select(line => 
                        StorageProviderHelpers.TryCreateBclStorageItem(line.Substring(7))!)
                    .ToList();

                DataReceived?.Invoke(DataFormats.Files, uris);
            }
            else if (dataType == _textPlain)
            {
                Encoding ansiEncoding = Encoding.GetEncoding(0); //system ANSI
                string data = ansiEncoding.GetString(_result.ToArray());

                DataReceived?.Invoke(DataFormats.Text, data);
            }
            else if (dataType == _atoms.UTF8_STRING)
            {
                string data = Encoding.UTF8.GetString(_result.ToArray());

                DataReceived?.Invoke(DataFormats.Text, data);
            }
            else
            {
                DataReceived?.Invoke(_atoms.GetAtomName(dataType) ?? string.Empty, _result);
            }

            _result.Clear();
        }
    }
}
