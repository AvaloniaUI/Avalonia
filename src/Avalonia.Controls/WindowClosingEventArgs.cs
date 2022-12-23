using System.ComponentModel;

namespace Avalonia.Controls
{
    /// <summary>
    /// Specifies the reason that a window was closed.
    /// </summary>
    public enum WindowCloseReason
    {
        /// <summary>
        /// The cause of the closure was not provided by the underlying platform.
        /// </summary>
        Undefined,

        /// <summary>
        /// The window itself was requested to close.
        /// </summary>
        WindowClosing,

        /// <summary>
        /// The window is closing due to a parent/owner window closing.
        /// </summary>
        OwnerWindowClosing,

        /// <summary>
        /// The window is closing due to the application shutting down.
        /// </summary>
        ApplicationShutdown,

        /// <summary>
        /// The window is closing due to the operating system shutting down.
        /// </summary>
        OSShutdown,
    }

    /// <summary>
    /// Provides data for the <see cref="Window.Closing"/> event.
    /// </summary>
    public class WindowClosingEventArgs : CancelEventArgs
    {
        internal WindowClosingEventArgs(WindowCloseReason reason, bool isProgrammatic)
        {
            CloseReason = reason;
            IsProgrammatic = isProgrammatic;
        }

        /// <summary>
        /// Gets a value that indicates why the window is being closed.
        /// </summary>
        public WindowCloseReason CloseReason { get; }

        /// <summary>
        /// Gets a value indicating whether the window is being closed programmatically.
        /// </summary>
        public bool IsProgrammatic { get; }
    }
}
