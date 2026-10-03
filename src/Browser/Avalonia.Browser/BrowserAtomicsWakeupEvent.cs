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
        var timeoutNs = timeout.HasValue
            ? (long)Math.Max(0, timeout.Value.TotalMilliseconds * 1_000_000)
            : -1;
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
