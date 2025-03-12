using System;
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
    }

    public void Dispose() => _window.Dispose();


    private async Task<(IntPtr propertyData, IntPtr actualTypeAtom, int actualFormat, IntPtr nitems)?> ConvertSelectionAndGetProperty(
        IntPtr target, IntPtr property)
    {
        XConvertSelection(_platform.Display, _x11.Atoms.CLIPBOARD, target, property, _window.Handle,
            IntPtr.Zero);
        
        var ev = await _window.WaitForEventAsync(ev =>
            ev.type == XEventName.SelectionNotify 
            && ev.SelectionEvent.selection == _x11.Atoms.CLIPBOARD
            && ev.SelectionEvent.property == property
            );
        
        if (ev == null)
            return null;
        
        var sel = ev.Value.SelectionEvent;
        
        XGetWindowProperty(_x11.Display, _window.Handle, sel.property, IntPtr.Zero, new IntPtr (0x7fffffff), true, 
            (IntPtr)Atom.AnyPropertyType,
            out var actualTypeAtom, out var actualFormat, out var nitems, out var bytes_after, out var prop);
        return (prop, actualTypeAtom, actualFormat, nitems);
    }
    
    public async Task<IntPtr[]?> SendFormatRequest()
    {
        var res = await ConvertSelectionAndGetProperty(_x11.Atoms.TARGETS, _x11.Atoms.TARGETS);
        if (res == null)
            return null;
        
        var (prop, _, actualFormat, nitems) = res.Value;
        
        try
        {
            if (nitems == IntPtr.Zero)
                return null;
            if (actualFormat != 32)
                return null;
            else
            {
                var formats = new IntPtr[nitems.ToInt32()];
                Marshal.Copy(prop, formats, 0, formats.Length);
                return formats;
            }
        }
        finally
        {
            XFree(prop);
        }
    }
    
    public class GetDataResult(byte[]? data, MemoryStream? stream, IntPtr actualTypeAtom)
    {
        public IntPtr TypeAtom => actualTypeAtom;
        public byte[] AsBytes() => data ?? stream!.ToArray();
        public MemoryStream AsStream() => stream ?? new MemoryStream(data!);
    }
    
    public async Task<GetDataResult?> SendDataRequest(IntPtr format)
    {
        var res = await ConvertSelectionAndGetProperty(format, format);
        if (res == null)
            return null;
        
        var (prop, actualTypeAtom, actualFormat, nitems) = res.Value;
        
        try
        {
            if (nitems == IntPtr.Zero)
                return null;
            if (actualTypeAtom == _x11.Atoms.INCR)
            {
                // TODO: Actually implement that monstrosity
                return null;
            }
            else
            {
                var data = new byte[(int)nitems * (actualFormat / 8)];
                Marshal.Copy(prop, data, 0, data.Length);
                return new (data, null, actualTypeAtom);
            }
        }
        finally
        {
            XFree(prop);
        }
    }
}