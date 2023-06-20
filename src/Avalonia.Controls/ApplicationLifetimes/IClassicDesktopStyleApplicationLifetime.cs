using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Controls.ApplicationLifetimes
{
    /// <summary>
    /// Controls application lifetime in classic desktop style
    /// </summary>
    [NotClientImplementable]
    public interface IClassicDesktopStyleApplicationLifetime : IControlledApplicationLifetime
    {
        /// <summary>
        /// Tries to Shutdown the application. <see cref="ShutdownRequested" /> event can be used to cancel the shutdown.
        /// </summary>
        /// <param name="exitCode">An integer exit code for an application. The default exit code is 0.</param>
        bool TryShutdown(int exitCode = 0);

        /// <summary>
        /// Gets the arguments passed to the
        /// <see cref="ClassicDesktopStyleApplicationLifetimeExtensions.StartWithClassicDesktopLifetime(AppBuilder, string[], ShutdownMode)"/>
        /// method.
        /// </summary>
        string[]? Args { get; }
        
        /// <summary>
        /// Gets or sets the <see cref="ShutdownMode"/>. This property indicates whether the application is shutdown explicitly or implicitly. 
        /// If <see cref="ShutdownMode"/> is set to OnExplicitShutdown the application is only closes if Shutdown is called.
        /// The default is OnLastWindowClose
        /// </summary>
        /// <value>
        /// The shutdown mode.
        /// </value>
        ShutdownMode ShutdownMode { get; set; }

        /// <summary>
        /// Gets or sets the main window of the application.
        /// </summary>
        /// <value>
        /// The main window.
        /// </value>
        Window? MainWindow { get; set; }

        /// <summary>
        /// Gets the list of all open windows in the application.
        /// </summary>
        IReadOnlyList<Window> Windows { get; }

        /// <summary>
        /// Raised by the platform when an application shutdown is requested.
        /// </summary>
        /// <remarks>
        /// Application Shutdown can be requested for various reasons like OS shutdown.
        /// 
        /// On Windows this will be called when an OS Session (logout or shutdown) terminates. Cancelling the eventargs will 
        /// block OS shutdown.
        /// 
        /// On OSX this has the same behavior as on Windows and in addition:
        /// This event is raised via the Quit menu or right-clicking on the application icon and selecting Quit. 
        /// 
        /// This event provides a first-chance to cancel application shutdown; if shutdown is not canceled at this point the application
        /// will try to close each non-owned open window, invoking the <see cref="Window.Closing"/> event on each and allowing
        /// each window to cancel the shutdown of the application. Windows cannot however prevent OS shutdown.
        /// </remarks>
        event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;
    }
}
