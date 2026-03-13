using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.X11.DragDrop;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Clipboard;

internal sealed class SelectionReadSession(
    IntPtr display,
    IntPtr window,
    IntPtr selection,
    IEventWaiter eventWaiter,
    X11Atoms atoms)
    : IDisposable
{
    public void Dispose() => eventWaiter.Dispose();

    private sealed class PropertyReadResult(IntPtr data, IntPtr actualTypeAtom, int actualFormat, IntPtr nItems)
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
        var ev = await eventWaiter.WaitForEventAsync(
            ev => ev.type == XEventName.SelectionNotify &&
                  ev.SelectionEvent.requestor == window &&
                  ev.SelectionEvent.selection == selection &&
                  ev.SelectionEvent.property == property,
            TimeSpan.FromSeconds(5));
        
        if (ev == null)
            return null;
        
        return ReadProperty(property);
    }

    private PropertyReadResult ReadProperty(IntPtr property)
    {
        XGetWindowProperty(display, window, property, IntPtr.Zero, new IntPtr (0x7fffffff), true,
            (IntPtr)Atom.AnyPropertyType,
            out var actualTypeAtom, out var actualFormat, out var nitems, out var bytes_after, out var prop);
        return new (prop, actualTypeAtom, actualFormat, nitems);
    }

    private Task<PropertyReadResult?> ConvertSelectionAndGetProperty(IntPtr target, IntPtr property, IntPtr timestamp)
    {
        XConvertSelection(display, selection, target, property, window, timestamp);
        return WaitForSelectionNotifyAndGetProperty(property);
    }
    
    public async Task<IntPtr[]?> SendFormatRequest(IntPtr targetsAtom)
    {
        using var res = await ConvertSelectionAndGetProperty(atoms.TARGETS, atoms.TARGETS, 0);
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
        XFlush(display);
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
            var ev = await eventWaiter.WaitForEventAsync(
                x => x is { type: XEventName.PropertyNotify, PropertyEvent.state: 0 } &&
                     x.PropertyEvent.window == window &&
                     x.PropertyEvent.atom == property,
                TimeSpan.FromSeconds(5));
            
            if (ev == null)
                return null;

            using var part = ReadProperty(property);

            if (actualTypeAtom == IntPtr.Zero)
                actualTypeAtom = part.ActualTypeAtom;
            if(part.NItems == IntPtr.Zero)
                break;
            
            Append(part);
        }

        ms.Position = 0L;
        return new(null, ms, actualTypeAtom);
    }
    
    public async Task<GetDataResult?> SendDataRequest(IntPtr format, IntPtr timestamp)
    {
        using var res = await ConvertSelectionAndGetProperty(format, format, timestamp);
        if (res == null)
            return null;
        
        if (res.NItems == IntPtr.Zero)
            return null;
        if (res.ActualTypeAtom == atoms.INCR)
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
