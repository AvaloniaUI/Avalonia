using System;

namespace Avalonia
{
    public class RenderTargetUnavailableException : Exception
    {
        public RenderTargetUnavailableException()
        {
        }

        public RenderTargetUnavailableException(string message)
            : base(message)
        {
        }

        public RenderTargetUnavailableException(Exception innerException)
            : base(null, innerException)
        {
        }

        public RenderTargetUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}