using System;

namespace Avalonia;

public class RenderTargetNotReadyException : Exception
{
    public RenderTargetNotReadyException()
    {
    }

    public RenderTargetNotReadyException(string message)
        : base(message)
    {
    }

    public RenderTargetNotReadyException(Exception innerException)
        : base(null, innerException)
    {
    }

    public RenderTargetNotReadyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
