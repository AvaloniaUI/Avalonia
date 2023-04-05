using System;
using System.Runtime.InteropServices;
using CoreFoundation;
using Foundation;
using ObjCRuntime;

namespace Avalonia.iOS;

// TODO: use LibraryImport in NET7
internal class Interop
{
    internal const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    internal static NativeHandle kCFRunLoopCommonModes = CFString.CreateNative("kCFRunLoopCommonModes");

    [Flags]
    public enum CFOptionFlags : ulong
    {
        kCFRunLoopBeforeSources = (1UL << 2),
        kCFRunLoopAfterWaiting = (1UL << 6),
        kCFRunLoopBeforeWaiting = (1UL << 5)
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void CFRunLoopObserverCallback(IntPtr observer, CFOptionFlags activity);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void CFRunLoopTimerCallback(IntPtr timer);

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFRunLoopObserverCreateWithHandler(IntPtr allocator, CFOptionFlags activities, bool repeats, int index, ref BlockLiteral block);

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFRunLoopAddObserver(IntPtr loop, IntPtr observer, IntPtr mode);

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFRunLoopTimerCreateWithHandler(IntPtr allocator, double firstDate, double interval,
        CFOptionFlags flags, int order, ref BlockLiteral block);

    [DllImport(CoreFoundationLibrary)]
    internal static extern void CFRunLoopTimerSetTolerance(IntPtr timer, double tolerance);

    [DllImport(CoreFoundationLibrary)]
    internal static extern void CFRunLoopTimerSetNextFireDate(IntPtr timer, double fireDate);
    
    [DllImport(CoreFoundationLibrary)]
    internal static extern double CFAbsoluteTimeGetCurrent();
}
