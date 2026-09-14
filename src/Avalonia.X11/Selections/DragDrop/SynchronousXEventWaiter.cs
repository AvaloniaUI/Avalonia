using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Selections.DragDrop;

/// <summary>
/// An implementation of <see cref="IXEventWaiter"/> that waits for an event synchronously by using its own event loop.
/// Unprocessed events are put back onto the main queue after the wait.
/// </summary>
internal sealed partial class SynchronousXEventWaiter(IntPtr display) : IXEventWaiter
{
    private const short POLLIN = 0x0001;

    public XEvent? WaitForEvent(Func<XEvent, bool> predicate, TimeSpan timeout)
    {
        var startingTimestamp = Stopwatch.GetTimestamp();
        TimeSpan elapsed;
        List<XEvent>? stashedEvents = null;
        var fd = 0;

        try {
            while (true)
            {
                if (!CheckTimeout())
                    return null;

                // First, wait until there's some event in the queue
                while (XPending(display) == 0)
                {
                    if (fd == 0)
                        fd = XConnectionNumber(display);

                    if (!CheckTimeout())
                        return null;

                    var remainingTimeout = timeout - elapsed;

                    var pollfd = new PollFd
                    {
                        fd = fd,
                        events = POLLIN
                    };

                    unsafe
                    {
                        if (poll(&pollfd, 1, (int)remainingTimeout.TotalMilliseconds) != 1)
                            return null;
                    }
                }

                if (!CheckTimeout())
                    return null;

                // Second, check if there's an event we're interested in.
                // Use XNextEvent and stash the unrelated events, then put them back onto the queue.
                // We can't use XIfEvent because it could block indefinitely.
                // We can't use XCheckIfEvent either: by leaving the event in the queue, the next poll()
                // returns immediately, eating CPU, and we don't want to add arbitrary thread sleeps.
                XNextEvent(display, out var evt);

                if (evt.type == XEventName.GenericEvent)
                {
                    unsafe
                    {
                        XGetEventData(display, &evt.GenericEventCookie);
                    }
                }

                if (predicate(evt))
                    return evt;

                stashedEvents ??= [];
                stashedEvents.Add(evt);
            }
        }
        finally
        {
            if (stashedEvents is not null)
            {
                foreach (var evt in stashedEvents)
                {
                    XPutBackEvent(display, evt);

                    if (evt.type == XEventName.GenericEvent)
                    {
                        unsafe
                        {
                            if (evt.GenericEventCookie.data != null)
                                XFreeEventData(display, &evt.GenericEventCookie);
                        }
                    }
                }
            }
        }

        bool CheckTimeout()
        {
            elapsed = Stopwatch.GetElapsedTime(startingTimestamp);
            return elapsed < timeout;
        }
    }

    Task<XEvent?> IXEventWaiter.WaitForEventAsync(Func<XEvent, bool> predicate, TimeSpan timeout)
        => Task.FromResult(WaitForEvent(predicate, timeout));

    void IDisposable.Dispose()
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PollFd
    {
        public int fd;
        public short events;
        public short revents;
    }

    [LibraryImport("libc", SetLastError = true)]
    private static unsafe partial int poll(PollFd* fds, nint nfds, int timeout);
}
