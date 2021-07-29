using System;
using System.ComponentModel;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Platform
{
    public interface IPlatformLifetimeEventsImpl
    {
        /// <summary>
        /// Raised by the platform when a shutdown is requested.
        /// </summary>
        /// <remarks>
        /// Raised on on OSX via the Quit menu or right-clicking on the application icon and selecting Quit.
        /// </remarks>
        event EventHandler<ShutdownRequestedEventArgs> ShutdownRequested;
    }
}
