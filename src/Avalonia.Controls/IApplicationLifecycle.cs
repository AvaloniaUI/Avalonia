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
        event EventHandler<StartupEventArgs> Startup;

        /// <summary>
        /// Sent when the application is exiting.
        /// </summary>
        event EventHandler<ExitEventArgs> Exit;

        /// <summary>
        /// Shuts down an application that returns the specified exit code to the operating system.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Shuts down an application that returns the specified exit code to the operating system.
        /// </summary>
        /// <param name="exitCode">An integer exit code for an application. The default exit code is 0.</param>
        void Shutdown(int exitCode);
    }
}
