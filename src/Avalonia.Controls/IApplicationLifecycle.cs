using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Sends events about the application lifecycle.
    /// </summary>
    public interface IApplicationLifecycle
    {
        /// <summary>
        /// Sent when the application is starting up.
        /// </summary>
        event EventHandler Startup;

        /// <summary>
        /// Sent when the application is exiting.
        /// </summary>
        event EventHandler Exit;

        /// <summary>
        /// Exits the application.
        /// </summary>
        void Shutdown();
    }
}
