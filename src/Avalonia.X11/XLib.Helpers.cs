using System;
using System.Runtime.InteropServices;

namespace Avalonia.X11;

internal static partial class XLib
{
    public static IntPtr[]? XGetWindowPropertyAsIntPtrArray(IntPtr display, IntPtr window, IntPtr atom, IntPtr reqType)
    {
        if (XGetWindowProperty(display, window, atom, IntPtr.Zero, new IntPtr(0x7fffffff),
                false, reqType, out var actualType, out var actualFormat, out var nitems, out _,
                out var prop) != 0)
            return null;

        try
        {
            if (actualType != reqType || actualFormat != 32 || nitems == IntPtr.Zero)
                return null;

            var buffer = new IntPtr[nitems.ToInt32()];
            Marshal.Copy(prop, buffer, 0, buffer.Length);
            return buffer;
        }
        finally
        {
            XFree(prop);
        }
    }
}