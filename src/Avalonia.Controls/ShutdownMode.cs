using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Controls
{
    /// <summary>
    /// Describes the possible values for <see cref="IClassicDesktopStyleApplicationLifetime.ShutdownMode"/>.
    /// </summary>
    public enum ShutdownMode
    {
        /// <summary>
        /// Indicates an implicit call to Application.Shutdown when the last window closes.
        /// </summary>
        OnLastWindowClose,

        /// <summary>
        /// Indicates an implicit call to Application.Shutdown when the main window closes.
        /// </summary>
        OnMainWindowClose,

        /// <summary>
        /// Indicates that the application only exits on an explicit call to Application.Shutdown.
        /// </summary>
        OnExplicitShutdown
    }
}
