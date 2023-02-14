using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IPlatformLifetimeEventsImpl
    {
        /// <summary>
        /// Raised by the platform when a shutdown is requested.
        /// </summary>
        /// <remarks>
        /// Raised on on OSX via the Quit menu or right-clicking on the application icon and selecting Quit.
        /// </remarks>
        event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;
    }
}
