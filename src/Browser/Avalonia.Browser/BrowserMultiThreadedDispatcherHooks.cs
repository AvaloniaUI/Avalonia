using System.Threading;

namespace Avalonia.Browser;

// Loop-thread hooks of the multithreaded dispatcher. They only enqueue dispatcher operations; the wake-up
// itself comes from JS via MultiThreadedDispatcherHelper.signal or from managed Signal calls.
internal static class BrowserMultiThreadedDispatcherHooks
{

    private static long s_wakeups;

    internal static long Wakeups => Interlocked.Read(ref s_wakeups);

    public static void OnBeforeWait()
    {
    }

    public static void OnAfterWakeup()
    {
        Interlocked.Increment(ref s_wakeups);
    }
}
