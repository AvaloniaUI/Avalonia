using System.Threading;
using Avalonia.Threading;

namespace Avalonia.Browser.Interop;

internal static class JsCallbackHelper
{
    // The browser runtime resets SynchronizationContext.Current on the main thread after every thread pool
    // work item, so code entered from JS cannot rely on the context installed at startup. Installs the
    // dispatcher context for the callback when the current thread runs a dispatcher; in MT mode callbacks
    // may arrive on a thread without one.
    public static AvaloniaSynchronizationContext.RestoreContext EnsureDispatcherContext()
    {
        if (Dispatcher.FromThread(Thread.CurrentThread) is not { } dispatcher)
            return default;
        return AvaloniaSynchronizationContext.Ensure(dispatcher, DispatcherPriority.Normal);
    }
}
