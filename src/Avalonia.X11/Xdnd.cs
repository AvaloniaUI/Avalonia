using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.X11;

using static XLib;

internal static class Xdnd
{
    public static int ProtocolVersion = 5;
}

/// Reference: https://freedesktop.org/wiki/Specifications/XDND/
internal class XdndDataObject : IDataObject
{
    private readonly X11Info _x11;

    private IEnumerable<string> KnownDataFormats { get; set; }
    private IntPtr MimeAtom { get; set; }

    internal XdndDataObject(X11Info x11, ref XEvent ev)
    {
        _x11 = x11;

        if (ev.type != XEventName.ClientMessage || ev.ClientMessageEvent.message_type != x11.Atoms.XdndEnter)
        {
            throw new ArgumentException($"Event has to be of type {nameof(X11Atoms.XdndEnter)}");
        }

        if (((ulong)ev.ClientMessageEvent.ptr2 & 1) == 0)
        {
            if (!TryAtom(ev.ClientMessageEvent.ptr3) &&
                !TryAtom(ev.ClientMessageEvent.ptr4) &&
                !TryAtom(ev.ClientMessageEvent.ptr5))
            {
                throw new Exception($"No supported Mimetype detected");
            }
        }
        else
        {
            var result = XGetWindowProperty(
                x11.Display,
                ev.ClientMessageEvent.ptr1,
                x11.Atoms.XdndTypeList,
                IntPtr.Zero,
                new IntPtr(1024),
                false,
                x11.Atoms.AnyPropertyType,
                out var type,
                out _,
                out var nItems,
                out _,
                out var prop
            );
            if (result != (int)Status.Success)
            {
                throw new Exception("XGetWindowProperty failed");
            }

            unsafe
            {
                var ptr = (IntPtr*)prop;
                if (type != IntPtr.Zero && prop != IntPtr.Zero && ptr != null)
                {
                    for (ulong i = 0; i < (ulong)nItems; ++i)
                    {
                        if (TryAtom(ptr[i])) break;
                    }
                }
            }

            XFree(prop);
        }
    }

    private bool TryAtom(IntPtr atom)
    {
        if (atom == _x11.Atoms.MimeTextUriList)
        {
            KnownDataFormats = new[] { DataFormats.Files };
            MimeAtom = atom;
            return true;
        }

        if (atom == _x11.Atoms.MimeTextUtf8 || atom == _x11.Atoms.MimeText)
        {
            KnownDataFormats = new[] { DataFormats.Text };
            MimeAtom = atom;
            return true;
        }

        return false;
    }

    private object Content { get; set; }

    public IEnumerable<string> GetDataFormats()
    {
        return KnownDataFormats;
    }

    public bool Contains(string dataFormat)
    {
        return KnownDataFormats.Contains(dataFormat);
    }

    public object Get(string dataFormat)
    {
        if (Content == null || !KnownDataFormats.Contains(dataFormat)) return null;

        return Content;
    }

    public void RequestContent(IntPtr timestamp, IntPtr window)
    {
        if (!KnownDataFormats.Any()) return;

        XConvertSelection(
            _x11.Display,
            _x11.Atoms.XdndSelection,
            MimeAtom,
            _x11.Atoms.XdndSelection,
            window,
            timestamp
        );
    }

    public void ReceiveContent(IntPtr window)
    {
        var result = XGetWindowProperty(
            _x11.Display,
            window,
            _x11.Atoms.XdndSelection,
            IntPtr.Zero,
            new IntPtr(1024),
            false,
            _x11.Atoms.AnyPropertyType,
            out var type,
            out _,
            out var nItems,
            out var remaining,
            out var prop
        );
        if (result != (int)Status.Success)
        {
            throw new Exception("XGetWindowProperty failed");
        }

        if (remaining != IntPtr.Zero)
        {
            XDeleteProperty(_x11.Display, window, prop);
            result = XGetWindowProperty(
                _x11.Display,
                window,
                _x11.Atoms.XdndSelection,
                IntPtr.Zero,
                remaining + 1024,
                false,
                _x11.Atoms.AnyPropertyType,
                out type,
                out _,
                out nItems,
                out remaining,
                out prop
            );
            if (result != (int)Status.Success)
            {
                throw new Exception("XGetWindowProperty failed");
            }
        }

        if (type != MimeAtom || prop == IntPtr.Zero)
        {
            XDeleteProperty(_x11.Display, window, prop);
            return;
        }

        var text =
            Marshal.PtrToStringAnsi(prop, (int)nItems);

        if (type == _x11.Atoms.MimeTextUriList)
        {
            Content =
                Uri.UnescapeDataString(text)
                    .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !line.StartsWith("#"))
                    .Select(line => line.StartsWith("file://") ? line.Substring(7) : line)
                    .Select(line => StorageProviderHelpers.TryCreateBclStorageItem(line)!)
                    .ToList();
        }
        else
        {
            Content = text;
        }

        XDeleteProperty(_x11.Display, window, prop);
    }
}
