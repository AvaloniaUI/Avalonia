using System;
using System.Runtime.InteropServices;

namespace GpuInterop;

static class NativeMethods
{
    [Flags]
    public enum IOSurfaceLockOptions : uint
    {
        None = 0,
        ReadOnly = 1 << 0,
        AvoidSync = 1 << 1,
    }

    [DllImport("/System/Library/Frameworks/IOSurface.framework/IOSurface")]
    public static extern int IOSurfaceLock(IntPtr surface, IOSurfaceLockOptions options, IntPtr seed);

    [DllImport("/System/Library/Frameworks/IOSurface.framework/IOSurface")]
    public static extern nint IOSurfaceGetWidth(IntPtr surface);

    [DllImport("/System/Library/Frameworks/IOSurface.framework/IOSurface")]
    public static extern nint IOSurfaceGetHeight(IntPtr surface);

    [DllImport("/System/Library/Frameworks/IOSurface.framework/IOSurface")]
    public static extern nint IOSurfaceGetBytesPerRow(IntPtr surface);

    [DllImport("/System/Library/Frameworks/IOSurface.framework/IOSurface")]
    public static extern IntPtr IOSurfaceGetBaseAddress(IntPtr surface);

    [DllImport("/System/Library/Frameworks/IOSurface.framework/IOSurface")]
    public static extern void IOSurfaceUnlock(IntPtr surface, IOSurfaceLockOptions options, IntPtr seed);
    
    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern IntPtr CFRetain(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern void CFRelease(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern nint CFGetRetainCount(IntPtr cf);
}
