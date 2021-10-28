using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Input;

namespace Avalonia
{
    /// <summary>
    /// Extension methods for attaching DevTools..
    /// </summary>
    public static class DevToolsExtensions
    {
        /// <summary>
        /// Attaches DevTools to a window, to be opened with the F12 key.
        /// </summary>
        /// <param name="root">The window to attach DevTools to.</param>
        /// <exception cref="System.ArgumentException">Throw ArgumentException is DevTools already attached to application.</exception> 
        public static void AttachDevTools(this TopLevel root)
        {
            DevTools.Attach(root, new DevToolsOptions());
        }

        /// <summary>
        /// Attaches DevTools to a window, to be opened with the specified key gesture.
        /// </summary>
        /// <param name="root">The window to attach DevTools to.</param>
        /// <param name="gesture">The key gesture to open DevTools.</param>
        /// <exception cref="System.ArgumentException">Throw ArgumentException is DevTools already attached to application.</exception> 
        public static void AttachDevTools(this TopLevel root, KeyGesture gesture)
        {
            DevTools.Attach(root, gesture);
        }

        /// <summary>
        /// Attaches DevTools to a window, to be opened with the specified options.
        /// </summary>
        /// <param name="root">The window to attach DevTools to.</param>
        /// <param name="options">Additional settings of DevTools.</param>
        /// <exception cref="System.ArgumentException">Throw ArgumentException is DevTools already attached to application.</exception> 
        public static void AttachDevTools(this TopLevel root, DevToolsOptions options)
        {
            DevTools.Attach(root, options);
        }

        /// <summary>
        /// Attaches DevTools to a Application, to be opened with the specified options.
        /// </summary>
        /// <param name="application">The Application to attach DevTools to.</param>
        /// <exception cref="System.ArgumentException">Throw ArgumentException if <paramref name="application"/> is already attached.</exception> 
        public static void AttachDevTools(this Application application)
        {
            DevTools.Attach(application, new DevToolsOptions());
        }

        /// <summary>
        /// Attaches DevTools to a Application, to be opened with the specified options.
        /// </summary>
        /// <param name="application">The Application to attach DevTools to.</param>
        /// <param name="options">Additional settings of DevTools.</param>
        /// <exception cref="System.ArgumentException">Throw ArgumentException if <paramref name="application"/> is already attached.</exception> 
        public static void AttachDevTools(this Application application, DevToolsOptions options)
        {
            DevTools.Attach(application, options);
        }
    }
}
