using System;
using Avalonia.Wayland.Server.Interop;

namespace Avalonia.Wayland;

public class AvaloniaWaylandException : Exception
{
    public AvaloniaWaylandException()
    {
    }

    public AvaloniaWaylandException(string? message) : base(message)
    {
    }

    public AvaloniaWaylandException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class AvaloniaWaylandPollException : AvaloniaWaylandException
{
    public AvaloniaWaylandPollException() : base("poll failed")
    {
    }

    public AvaloniaWaylandPollException(string? message) : base(message)
    {
    }

    public AvaloniaWaylandPollException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class AvaloniaWaylandNetworkException : AvaloniaWaylandException
{
    public AvaloniaWaylandNetworkException()
    {
    }

    public AvaloniaWaylandNetworkException(string? message) : base(message)
    {
    }

    public AvaloniaWaylandNetworkException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class AvaloniaWaylandFlushException : AvaloniaWaylandNetworkException
{
    public AvaloniaWaylandFlushException()
    {
    }

    public AvaloniaWaylandFlushException(string? message) : base(message)
    {
    }

    public AvaloniaWaylandFlushException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    internal AvaloniaWaylandFlushException(UnsafeNativeMethods.Errno errno) : base("wl_display_flush failed, errno: " + errno)
    {
    }
}

public class AvaloniaWaylandReadException : AvaloniaWaylandNetworkException
{
    public AvaloniaWaylandReadException()
    {
    }

    public AvaloniaWaylandReadException(string? message) : base(message)
    {
    }

    public AvaloniaWaylandReadException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    internal AvaloniaWaylandReadException(UnsafeNativeMethods.Errno errno) : base("wl_display_read_events failed, errno: " + errno)
    {
    }
}

public class AvaloniaWaylandProtocolErrorException : AvaloniaWaylandException
{
    public AvaloniaWaylandProtocolErrorException() : base("protocol error")
    {
    }

    public AvaloniaWaylandProtocolErrorException(string? message) : base(message)
    {
    }

    public AvaloniaWaylandProtocolErrorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    internal AvaloniaWaylandProtocolErrorException(uint errorCode, string errorMessage) : base($"protocol error: {errorCode} {errorMessage}")
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public uint ErrorCode { get; }
    public string? ErrorMessage { get; }
}
