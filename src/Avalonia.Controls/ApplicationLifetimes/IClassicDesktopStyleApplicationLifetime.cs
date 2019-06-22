using System;
using System.Collections.Generic;

namespace Avalonia.Controls.ApplicationLifetimes
{
    /// <summary>
    /// Controls application lifetime in classic desktop style
    /// </summary>
    public interface IClassicDesktopStyleApplicationLifetime : IControlledApplicationLifetime
    {
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
        Window MainWindow { get; set; }
        
        IReadOnlyList<Window> Windows { get; }
    }
}
