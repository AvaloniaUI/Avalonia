using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;

namespace Avalonia.Browser;

// Multithreaded builds only: backed by native/avalonia_browser_dispatcher.c so JS on the main thread can
// signal it without entering managed code. Wait must run on the dispatcher pthread (Atomics.wait is not
// allowed on the browser main thread).
internal sealed class BrowserAtomicsWakeupEvent : ManagedDispatcherImpl.IWakeupEvent, IDisposable
{
    private IntPtr _ptr;

    public BrowserAtomicsWakeupEvent()
    {
        _ptr = avn_wakeup_create();
        if (_ptr == IntPtr.Zero)
            throw new OutOfMemoryException();
    }

    public IntPtr Pointer => _ptr;

    public void Set() => avn_wakeup_set(_ptr);

    public void Wait(TimeSpan? timeout)
    {
        // Integer ticks avoid the double round-trip; a timeout too long to express in nanoseconds waits forever.
        var timeoutNs = timeout?.Ticks switch
        {
            null => -1,
            <= 0 => 0,
            >= long.MaxValue / TimeSpan.NanosecondsPerTick => -1,
            { } ticks => ticks * TimeSpan.NanosecondsPerTick
        };
        avn_wakeup_wait(_ptr, timeoutNs);
    }

    public void Dispose()
    {
        if (_ptr == IntPtr.Zero)
            return;
        avn_wakeup_destroy(_ptr);
        _ptr = IntPtr.Zero;
    }

    private const string Library = "avalonia_browser_dispatcher";

    [DllImport(Library)]
    private static extern IntPtr avn_wakeup_create();

    [DllImport(Library)]
    private static extern void avn_wakeup_destroy(IntPtr p);

    [DllImport(Library)]
    private static extern void avn_wakeup_set(IntPtr p);

    [DllImport(Library)]
    private static extern int avn_wakeup_wait(IntPtr p, long timeoutNs);
}
