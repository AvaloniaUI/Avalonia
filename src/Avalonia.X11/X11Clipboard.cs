using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    internal class X11Clipboard : IClipboard
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly X11Info _x11;
        private IDataObject _storedDataObject;
        private IntPtr _handle;
        private TaskCompletionSource<bool> _storeAtomTcs;
        private TaskCompletionSource<IntPtr[]> _requestedFormatsTcs;
        private TaskCompletionSource<object> _requestedDataTcs;
        private readonly IntPtr[] _textAtoms;
        private readonly IntPtr _avaloniaSaveTargetsAtom;
        private IntPtr _incrReadTargetAtom;
        private readonly List<byte> _incrReadData;
        private IntPtr _incrWriteTargetAtom;
        private IntPtr _incrWriteWindow;
        private IntPtr _incrWriteProperty;
        private byte[] _incrWriteData;
        private const int MaxRequestSize = 0x40000;

        public X11Clipboard(AvaloniaX11Platform platform)
        {
            _platform = platform;
            _x11 = platform.Info;
            _handle = CreateEventWindow(platform, OnEvent);
            XSelectInput(_x11.Display, _handle, new IntPtr((int)(EventMask.StructureNotifyMask | EventMask.PropertyChangeMask)));

            _avaloniaSaveTargetsAtom = XInternAtom(_x11.Display, "AVALONIA_SAVE_TARGETS_PROPERTY_ATOM", false);
            _textAtoms = new[]
            {
                _x11.Atoms.XA_STRING,
                _x11.Atoms.OEMTEXT,
                _x11.Atoms.UTF8_STRING,
                _x11.Atoms.UTF16_STRING
            }.Where(a => a != IntPtr.Zero).ToArray();

            _incrReadTargetAtom = IntPtr.Zero;
            _incrReadData = new List<byte>();
            _incrWriteTargetAtom = IntPtr.Zero;
            _incrWriteWindow = IntPtr.Zero;
            _incrWriteProperty = IntPtr.Zero;
            _incrWriteData = null;
        }

        private bool IsStringAtom(IntPtr atom)
        {
            return _textAtoms.Contains(atom);
        }

        private Encoding GetStringEncoding(IntPtr atom)
        {
            return (atom == _x11.Atoms.XA_STRING
                    || atom == _x11.Atoms.OEMTEXT)
                ? Encoding.ASCII
                : atom == _x11.Atoms.UTF8_STRING
                    ? Encoding.UTF8
                    : atom == _x11.Atoms.UTF16_STRING
                        ? Encoding.Unicode
                        : null;
        }

        private unsafe void OnEvent(ref XEvent ev)
        {
            if (ev.type == XEventName.SelectionRequest)
            {
                var sel = ev.SelectionRequestEvent;
                var resp = new XEvent
                {
                    SelectionEvent =
                    {
                        type = XEventName.SelectionNotify,
                        send_event = 1,
                        display = _x11.Display,
                        selection = sel.selection,
                        target = sel.target,
                        requestor = sel.requestor,
                        time = sel.time,
                        property = IntPtr.Zero
                    }
                };
                if (sel.selection == _x11.Atoms.CLIPBOARD)
                {
                    resp.SelectionEvent.property = WriteTargetToProperty(sel.target, sel.requestor, sel.property);
                }

                XSendEvent(_x11.Display, sel.requestor, false, new IntPtr((int)EventMask.NoEventMask), ref resp);
                return;
            }

            if (ev.type == XEventName.SelectionNotify && ev.SelectionEvent.selection == _x11.Atoms.CLIPBOARD)
            {
                var sel = ev.SelectionEvent;
                if (sel.property == IntPtr.Zero)
                {
                    _requestedFormatsTcs?.TrySetResult(null);
                    _requestedDataTcs?.TrySetResult(null);
                    return;
                }
                XGetWindowProperty(_x11.Display, _handle, sel.property, IntPtr.Zero, new IntPtr(0x7fffffff), true, (IntPtr)Atom.AnyPropertyType,
                    out var actualTypeAtom, out var actualFormat, out var nitems, out var bytes_after, out var prop);
                Encoding textEnc = null;
                if (nitems == IntPtr.Zero)
                {
                    _requestedFormatsTcs?.TrySetResult(null);
                    _requestedDataTcs?.TrySetResult(null);
                }
                else
                {
                    if (sel.property == _x11.Atoms.TARGETS)
                    {
                        if (actualFormat != 32)
                            _requestedFormatsTcs?.TrySetResult(null);
                        else
                        {
                            var formats = new IntPtr[nitems.ToInt32()];
                            Marshal.Copy(prop, formats, 0, formats.Length);
                            _requestedFormatsTcs?.TrySetResult(formats);
                        }
                    }
                    else if ((textEnc = GetStringEncoding(actualTypeAtom)) != null)
                    {
                        var text = textEnc.GetString((byte*)prop.ToPointer(), nitems.ToInt32());
                        _requestedDataTcs?.TrySetResult(text);
                    }
                    else
                    {
                        if (actualTypeAtom == _x11.Atoms.INCR)
                        {
                            if (actualFormat != 32 || (int)nitems != 1)
                                _requestedDataTcs?.TrySetResult(null);
                            else
                            {
                                _incrReadTargetAtom = sel.property;
                                var total = *((int*)prop.ToPointer());
                                _incrReadData.Capacity = total;
                            }
                        }
                        else
                        {
                            var data = new byte[(int)nitems * (actualFormat / 8)];
                            Marshal.Copy(prop, data, 0, data.Length);
                            _requestedDataTcs?.TrySetResult(data);
                        }
                    }
                }

                XFree(prop);
                return;
            }

            if (ev.type == XEventName.PropertyNotify)
            {
                if (_incrReadTargetAtom == ev.PropertyEvent.atom && (PropertyState)ev.PropertyEvent.state == PropertyState.NewValue)
                {
                    XGetWindowProperty(_x11.Display, _handle, _incrReadTargetAtom, IntPtr.Zero, new IntPtr(0x7fffffff), true, (IntPtr)Atom.AnyPropertyType,
                                out var actualTypeAtom, out var actualFormat, out var nitems, out var bytes_after, out var prop);

                    if (_incrReadTargetAtom == actualTypeAtom && (int)nitems > 0)
                    {
                        var chunkSize = (int)nitems * (actualFormat / 8);
                        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(chunkSize);
                        Marshal.Copy(prop, buffer, 0, chunkSize);
                        _incrReadData.AddRange(buffer.Take(chunkSize));
                    }
                    else
                    {
                        var bytes = _incrReadData.ToArray();
                        var textEnc = GetStringEncoding(_incrReadTargetAtom);
                        _incrReadData.Clear();
                        _incrReadTargetAtom = IntPtr.Zero;

                        if (bytes.Length == 0)
                        {
                            _requestedDataTcs?.TrySetResult(null);
                        }
                        else if (textEnc != null)
                        {
                            var text = textEnc.GetString(bytes);
                            _requestedDataTcs?.TrySetResult(text);
                        }
                        else
                        {
                            _requestedDataTcs?.TrySetResult(bytes);
                        }
                    }
                    XFree(prop);
                    return;
                }
            }
        }

        private void OnIncrWritePropertyEvent(ref XEvent ev)
        {
            if (ev.type == XEventName.PropertyNotify && (PropertyState)ev.PropertyEvent.state == PropertyState.Delete && ev.PropertyEvent.atom == _incrWriteTargetAtom)
            {
                if (_incrWriteData?.Length > 0)
                {
                    var bytes = _incrWriteData.Take(MaxRequestSize).ToArray();
                    _incrWriteData = _incrWriteData.Skip(bytes.Length).ToArray();
                    XChangeProperty(_x11.Display, _incrWriteWindow, _incrWriteProperty, _incrWriteTargetAtom, 8, PropertyMode.Replace, bytes, bytes.Length);
                }
                else
                {
                    _platform.Windows.Remove(_incrWriteWindow);
                    XChangeProperty(_x11.Display, _incrWriteWindow, _incrWriteProperty, _incrWriteTargetAtom, 8, PropertyMode.Replace, IntPtr.Zero, 0);
                    _incrWriteTargetAtom = IntPtr.Zero;
                    _incrWriteData = null;
                    _storeAtomTcs?.TrySetResult(true);
                }
            }
        }

        private unsafe IntPtr WriteTargetToProperty(IntPtr target, IntPtr window, IntPtr property)
        {
            if (target == _x11.Atoms.TARGETS)
            {
                var atoms = ConvertDataObject(_storedDataObject);
                XChangeProperty(_x11.Display, window, property,
                    _x11.Atoms.XA_ATOM, 32, PropertyMode.Replace, atoms, atoms.Length);
                return property;
            }

            if (target == _x11.Atoms.SAVE_TARGETS && _x11.Atoms.SAVE_TARGETS != IntPtr.Zero)
            {
                return property;
            }

            if (target == _x11.Atoms.MULTIPLE && _x11.Atoms.MULTIPLE != IntPtr.Zero)
            {
                XGetWindowProperty(_x11.Display, window, property, IntPtr.Zero, new IntPtr(0x7fffffff), false,
                    _x11.Atoms.ATOM_PAIR, out _, out var actualFormat, out var nitems, out _, out var prop);

                if (nitems != IntPtr.Zero && actualFormat == 32)
                {
                    var data = (IntPtr*)prop.ToPointer();
                    for (var c = 0; c < nitems.ToInt32(); c += 2)
                    {
                        var subTarget = data[c];
                        var subProp = data[c + 1];
                        var converted = WriteTargetToProperty(subTarget, window, subProp);
                        data[c + 1] = converted;
                    }

                    XChangeProperty(_x11.Display, window, property, _x11.Atoms.ATOM_PAIR, 32, PropertyMode.Replace,
                        prop.ToPointer(), nitems.ToInt32());
                }

                XFree(prop);
                return property;
            }

            if (_storedDataObject?.Contains(DataFormats.Text) == true || _storedDataObject?.Contains(_x11.Atoms.GetAtomName(target)) == true)
            {
                var objValue = _storedDataObject.Get(DataFormats.Text) ?? _storedDataObject.Get(_x11.Atoms.GetAtomName(target));

                if (!(objValue is byte[] bytes))
                {
                    if (objValue is string s)
                    {
                        var textEnc = GetStringEncoding(target) ?? Encoding.UTF8;
                        bytes = textEnc.GetBytes(s);
                    }
                    else
                        return IntPtr.Zero;
                }

                if (bytes.Length > MaxRequestSize)
                {
                    _incrWriteTargetAtom = target;
                    _incrWriteWindow = window;
                    _incrWriteProperty = property;
                    _incrWriteData = bytes;
                    _platform.Windows[window] = OnIncrWritePropertyEvent;
                    var total = new IntPtr[] { (IntPtr)bytes.Length };
                    XChangeProperty(_x11.Display, window, property, _x11.Atoms.INCR, 32, PropertyMode.Replace, total, total.Length);
                    XSelectInput(_x11.Display, window, new IntPtr((int)EventMask.PropertyChangeMask));
                }
                else
                {
                    XChangeProperty(_x11.Display, window, property, target, 8, PropertyMode.Replace, bytes, bytes.Length);
                    _storeAtomTcs?.TrySetResult(true);
                }

                return property;
            }

            return IntPtr.Zero;
        }

        private Task<IntPtr[]> SendFormatRequest()
        {
            if (_requestedFormatsTcs == null || _requestedFormatsTcs.Task.IsCompleted)
                _requestedFormatsTcs = new TaskCompletionSource<IntPtr[]>();
            XConvertSelection(_x11.Display, _x11.Atoms.CLIPBOARD, _x11.Atoms.TARGETS, _x11.Atoms.TARGETS, _handle,
                IntPtr.Zero);
            return _requestedFormatsTcs.Task;
        }

        private Task<object> SendDataRequest(IntPtr format)
        {
            if (_requestedDataTcs == null || _requestedDataTcs.Task.IsCompleted)
                _requestedDataTcs = new TaskCompletionSource<object>();
            XConvertSelection(_x11.Display, _x11.Atoms.CLIPBOARD, format, format, _handle, IntPtr.Zero);
            return _requestedDataTcs.Task;
        }

        private bool HasOwner => XGetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD) != IntPtr.Zero;

        public async Task<string> GetTextAsync()
        {
            if (!HasOwner)
                return null;
            var res = await SendFormatRequest();
            var target = _x11.Atoms.UTF8_STRING;
            if (res != null)
            {
                var preferredFormats = new[] { _x11.Atoms.UTF16_STRING, _x11.Atoms.UTF8_STRING, _x11.Atoms.XA_STRING };
                foreach (var pf in preferredFormats)
                    if (res.Contains(pf))
                    {
                        target = pf;
                        break;
                    }
            }

            return (string)await SendDataRequest(target);
        }

        private IntPtr[] ConvertDataObject(IDataObject data)
        {
            var atoms = new HashSet<IntPtr> { _x11.Atoms.TARGETS, _x11.Atoms.MULTIPLE };
            foreach (var fmt in data.GetDataFormats())
            {
                if (fmt == DataFormats.Text)
                    foreach (var ta in _textAtoms)
                        atoms.Add(ta);
                else
                    atoms.Add(_x11.Atoms.GetAtom(fmt));
            }
            return atoms.ToArray();
        }

        private void StoreAtomsInClipboardManager(IDataObject data)
        {
            if (_x11.Atoms.CLIPBOARD_MANAGER != IntPtr.Zero && _x11.Atoms.SAVE_TARGETS != IntPtr.Zero)
            {
                var clipboardManager = XGetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD_MANAGER);
                if (clipboardManager != IntPtr.Zero)
                {
                    var atoms = ConvertDataObject(data);
                    XChangeProperty(_x11.Display, _handle, _avaloniaSaveTargetsAtom, _x11.Atoms.XA_ATOM, 32,
                        PropertyMode.Replace,
                        atoms, atoms.Length);
                    XConvertSelection(_x11.Display, _x11.Atoms.CLIPBOARD_MANAGER, _x11.Atoms.SAVE_TARGETS,
                        _avaloniaSaveTargetsAtom, _handle, IntPtr.Zero);
                }
            }
        }

        private bool UseIncrProtocol(IDataObject data)
        {
            foreach (var fmt in data.GetDataFormats())
            {
                var objValue = _storedDataObject.Get(fmt);
                var dataSize = objValue switch
                {
                    byte[] bytes => bytes.Length,
                    string str => str.Length,
                    _ => 0
                };
                return dataSize > MaxRequestSize;
            }
            return false;
        }

        public Task SetTextAsync(string text)
        {
            var data = new DataObject();
            data.Set(DataFormats.Text, text);
            return SetDataObjectAsync(data);
        }

        public Task ClearAsync()
        {
            return SetTextAsync(null);
        }

        public Task SetDataObjectAsync(IDataObject data)
        {
            _storedDataObject = data;
            if (_storeAtomTcs == null || _storeAtomTcs.Task.IsCompleted)
                _storeAtomTcs = new TaskCompletionSource<bool>();

            XSetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD, _handle, IntPtr.Zero);

            if (!UseIncrProtocol(data))
                StoreAtomsInClipboardManager(data);

            return _storeAtomTcs.Task;
        }

        public async Task<string[]> GetFormatsAsync()
        {
            if (!HasOwner)
                return null;
            var res = await SendFormatRequest();
            if (res == null)
                return null;
            var rv = new List<string>();
            if (_textAtoms.Any(res.Contains))
                rv.Add(DataFormats.Text);
            foreach (var t in res)
                rv.Add(_x11.Atoms.GetAtomName(t));
            return rv.ToArray();
        }

        public async Task<object> GetDataAsync(string format)
        {
            if (!HasOwner)
                return null;
            if (format == DataFormats.Text)
                return await GetTextAsync();

            var formatAtom = _x11.Atoms.GetAtom(format);
            var res = await SendFormatRequest();
            if (!res.Contains(formatAtom))
                return null;

            return await SendDataRequest(formatAtom);
        }
    }
}
