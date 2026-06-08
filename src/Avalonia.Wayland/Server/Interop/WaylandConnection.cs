using System;
using System.Runtime.InteropServices;
using NWayland;
using NWayland.Protocols.Wayland;
using static Avalonia.Wayland.Server.Interop.UnsafeNativeMethods;
namespace Avalonia.Wayland.Server.Interop;

class WaylandConnection : IDisposable
{
    public WlDisplay Display { get; private set; }
    public WlEventQueue Queue { get; private set; }
    public bool IsConnected { get; private set; } = true;
    private readonly bool _ownsDisplay;
    private readonly int _fd;
    
    public WaylandConnection(string? path)
    {
        _ownsDisplay = true;
        Display = WlDisplay.Connect(path);
        Queue = Display.CreateEventQueue();
        _fd = Display.GetFd();
    }

    public WaylandConnection(int fd)
    {
        _ownsDisplay = true;
        Display = WlDisplay.ConnectToFd(fd);
        Queue = Display.CreateEventQueue();
        _fd = Display.GetFd();
    }
    
    public WaylandConnection(WlDisplay foreignDisplay)
    {
        Display = foreignDisplay;
        Queue = Display.CreateEventQueue();
        _fd = Display.GetFd();
    }

    public void Dispose()
    {
        IsConnected = false;
        Queue.Dispose();
        if (_ownsDisplay)
            Display.Dispose();
    }

        
    public void DispatchQueueOrWakeup(int wakeupFd)
    {
	    DispatchQueueOrWakeup(Queue, wakeupFd);
    }


    unsafe void DoPoll(Span<pollfd> fds)
    {
        while (true)
        {
            int pollRet;
            fixed (pollfd* fdsPtr = fds)
                pollRet = ppoll(fdsPtr, new IntPtr(fds.Length), IntPtr.Zero, IntPtr.Zero);
            if (pollRet <= 0)
            {
                var errno = Marshal.GetLastPInvokeError();
                // we need to check for this because ppoll ignores SA_RESTART set by .NET runtime
                if (errno == (int)Errno.EINTR)
                    continue;
                
                // Release the display read lock
                Display.CancelRead();
                // This is quite likely a non-recoverable error
                throw new AvaloniaWaylandPollException();
            }
            return;
        }
    }

    bool FlushDisplay(out Errno flushError)
    {
        var flushRet = Display.Flush();
        if (flushRet >= 0) // Successful flush
        {
            flushError = default;
            return true;
        }
        
        flushError = (Errno)Marshal.GetLastPInvokeError();

        if (flushError == Errno.EAGAIN)
        {
            // Not an error, just network buffers being full. We need to read our side of the socket and/or wait
            // for the compositor to process the previous requests, so report success
            flushError = default;
            return true;
        }
        
        return false;

    }
    
    public enum DispatchResult
    {
        Dispatched,
        Wakeup,
        ConnectionReset
    }

    bool IsConnectionReset(Errno errno) => errno is Errno.EPIPE or Errno.ECONNRESET;

    bool ClassifyErrorOrThrow(Errno errno, out DispatchResult result)
    {
        if (IsConnectionReset(errno))
        {
            result = DispatchResult.ConnectionReset;
            return true;
        }

        if (errno == Errno.EPROTO)
        {
            throw new AvaloniaWaylandProtocolErrorException();
        }

        result = default;
        return false;
    }
    
    public unsafe DispatchResult DispatchQueueOrWakeup(WlEventQueue queue, int wakeupFd)
    {
        // This initiates the race of sorts for who will have to use the wayland socket
        if (Queue.PrepareRead() == -1)
        {
            // Other thread has won the race and read from the display, we can safely dispatch pending events
            queue.DispatchPending();
            return DispatchResult.Dispatched;
        }
        
        // We won the race and are now responsible to drive libwayland's flush/poll/read cycle
        
        
        // First flush any pending data to the compositor
        if (!FlushDisplay(out var flushError))
        {
            // If somebody wants to read errors they should call Display.Dispatch() 
            Display.CancelRead();
            if (ClassifyErrorOrThrow(flushError, out var dispatchResult))
                return dispatchResult;
            
            throw new AvaloniaWaylandFlushException(flushError);
        }


        Span<pollfd> fds = stackalloc pollfd[]
        {
            new pollfd() { fd = Display.GetFd(), events = PollEvents.POLLIN },
            new pollfd() { fd = wakeupFd, events = PollEvents.POLLIN },
        };
        DoPoll(fds);

        if (fds[0].revents == default)
        {
            // ppoll got woken up by the wakeup fd
            Display.CancelRead();
            return DispatchResult.Wakeup;
        }

        // Actual read call
        if(Display.ReadEvents() == -1)
        {
            var errno = (Errno)Marshal.GetLastPInvokeError();
            if (ClassifyErrorOrThrow(errno, out var dispatchResult))
                return dispatchResult;
            throw new AvaloniaWaylandReadException(errno);
        }

        Queue.DispatchPending();
        return DispatchResult.Dispatched;
    }
}