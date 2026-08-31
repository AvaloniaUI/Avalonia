using System;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;

namespace ControlCatalog.Desktop;

internal static partial class ObjC
{
    private const string LibObjC = "/usr/lib/libobjc.dylib";

    public static IntPtr CreateString(string value)
    {
        using var utf8 = new Utf8Buffer(value);
        return MsgSend(GetClass("NSString"), GetUid("stringWithUTF8String:"), utf8);
    }

    [LibraryImport(LibObjC, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr GetClass(string name);

    [LibraryImport(LibObjC, EntryPoint = "sel_getUid", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr GetUid(string selector);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    internal static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    internal static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector, IntPtr arg);
}
