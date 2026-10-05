using System.ComponentModel;

namespace Avalonia.Controls.ApplicationLifetimes
{
    public class ShutdownRequestedEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Is the operating system shutting down
        /// </summary>
        internal bool IsOSShutdown { get; init; }

        /// <summary>
        /// Indicates that the accepted shutdown will exit the main loop.
        /// </summary>
        internal bool WillExitMainLoop { get; set; }
    }
}
