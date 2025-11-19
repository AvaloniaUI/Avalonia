using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Clipboard;

class ClipboardReadSession : IDisposable
{
    private readonly AvaloniaX11Platform _platform;
    private readonly EventStreamWindow _window;
    private readonly X11Info _x11;

    public ClipboardReadSession(AvaloniaX11Platform platform)
    {
        _platform = platform;
        _window = new EventStreamWindow(platform);
        _x11 = _platform.Info;
        XSelectInput(_x11.Display, _window.Handle, new IntPtr((int)XEventMask.PropertyChangeMask));
    }

    public void Dispose() => _window.Dispose();

    class PropertyReadResult(IntPtr data, IntPtr actualTypeAtom, int actualFormat, IntPtr nItems)
        : IDisposable
    {
        public IntPtr Data => data;
        public IntPtr ActualTypeAtom => actualTypeAtom;
        public int ActualFormat => actualFormat;
        public IntPtr NItems => nItems;
        
        public void Dispose()
        {
            XFree(Data);
        }
    }

    private async Task<PropertyReadResult?>
        WaitForSelectionNotifyAndGetProperty(IntPtr property)
    {
        var ev = await _window.WaitForEventAsync(ev =>
            ev.type == XEventName.SelectionNotify 
            && ev.SelectionEvent.selection == _x11.Atoms.CLIPBOARD
            && ev.SelectionEvent.property == property
        );
        
        if (ev == null)
            return null;
        
        var sel = ev.Value.SelectionEvent;
        
        return ReadProperty(sel.property);
    }

    private PropertyReadResult ReadProperty(IntPtr property)
    {
        XGetWindowProperty(_x11.Display, _window.Handle, property, IntPtr.Zero, new IntPtr (0x7fffffff), true, 
            (IntPtr)Atom.AnyPropertyType,
            out var actualTypeAtom, out var actualFormat, out var nitems, out var bytes_after, out var prop);
        return new (prop, actualTypeAtom, actualFormat, nitems);
    }

    private Task<PropertyReadResult?> ConvertSelectionAndGetProperty(
        IntPtr target, IntPtr property)
    {
        XConvertSelection(_platform.Display, _x11.Atoms.CLIPBOARD, target, property, _window.Handle,
            IntPtr.Zero);
        return WaitForSelectionNotifyAndGetProperty(property);
    }
    
    public async Task<IntPtr[]?> SendFormatRequest()
    {
        using var res = await ConvertSelectionAndGetProperty(_x11.Atoms.TARGETS, _x11.Atoms.TARGETS);
        if (res == null)
            return null;
        
        if (res.NItems == IntPtr.Zero)
            return null;
        if (res.ActualFormat != 32)
            return null;
        else
        {
            var formats = new IntPtr[res.NItems.ToInt32()];
            Marshal.Copy(res.Data, formats, 0, formats.Length);
            return formats;
        }
    }
    
    public class GetDataResult(byte[]? data, MemoryStream? stream, IntPtr actualTypeAtom)
    {
        public IntPtr TypeAtom => actualTypeAtom;
        public byte[] AsBytes() => data ?? stream!.ToArray();
        public MemoryStream AsStream() => stream ?? new MemoryStream(data!);
    }

    private async Task<GetDataResult?> ReadIncr(IntPtr property)
    {
        XFlush(_platform.Display);
        var ms = new MemoryStream();
        void Append(PropertyReadResult res)
        {
            var len = (int)res.NItems * (res.ActualFormat / 8);
            var data = ArrayPool<byte>.Shared.Rent(len);
            Marshal.Copy(res.Data, data, 0, len);
            ms.Write(data, 0, len);
            ArrayPool<byte>.Shared.Return(data);
        }
        IntPtr actualTypeAtom = IntPtr.Zero;
        while (true)
        {
            var ev = await _window.WaitForEventAsync(x =>
                x is { type: XEventName.PropertyNotify, PropertyEvent.state: 0 } &&
                x.PropertyEvent.atom == property);
            
            if (ev == null)
                return null;

            using var part = ReadProperty(property);

            if (actualTypeAtom == IntPtr.Zero)
                actualTypeAtom = part.ActualTypeAtom;
            if(part.NItems == IntPtr.Zero)
                break;
            
            Append(part);
        }

        return new(null, ms, actualTypeAtom);
    }
    
    public async Task<GetDataResult?> SendDataRequest(IntPtr format)
    {
        using var res = await ConvertSelectionAndGetProperty(format, format);
        if (res == null)
            return null;
        
        if (res.NItems == IntPtr.Zero)
            return null;
        if (res.ActualTypeAtom == _x11.Atoms.INCR)
        {
            return await ReadIncr(format);
        }
        else
        {
            var data = new byte[(int)res.NItems * (res.ActualFormat / 8)];
            Marshal.Copy(res.Data, data, 0, data.Length);
            return new (data, null, res.ActualTypeAtom);
        }
        
    }
}