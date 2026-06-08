using System;
using Avalonia.Wayland.Server.Interop;

namespace Avalonia.Wayland;

public class AvaloniaWaylandException : Exception
{
    public AvaloniaWaylandException(string message) : base(message)
    {
        
    }
}

public class AvaloniaWaylandPollException() : AvaloniaWaylandException("ppoll failed")
{
    
}

public class AvaloniaWaylandNetworkException(string message) : AvaloniaWaylandException(message)
{
    
}

public class AvaloniaWaylandFlushException : AvaloniaWaylandNetworkException
{
    internal AvaloniaWaylandFlushException(UnsafeNativeMethods.Errno errno) : base("wl_display_flush failed, errno: " + errno)
    {
    }
}


public class AvaloniaWaylandReadException : AvaloniaWaylandNetworkException
{
    internal AvaloniaWaylandReadException(UnsafeNativeMethods.Errno errno) : base("wl_display_read_events failed" + errno)
    {
    }
}

public class AvaloniaWaylandProtocolErrorException : AvaloniaWaylandException
{
    internal AvaloniaWaylandProtocolErrorException(uint errorCode, string errorMessage) : base($"protocol error: {errorCode} {errorMessage}")
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    internal AvaloniaWaylandProtocolErrorException() : base("protocol error")
    {
        
    }

    public uint ErrorCode { get; }
    public string? ErrorMessage { get; }
}