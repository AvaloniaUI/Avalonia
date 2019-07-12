using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    public static class DesktopApplicationExtensions
    {
        [Obsolete("Running application without a cancellation token and a lifetime is no longer supported, see https://github.com/AvaloniaUI/Avalonia/wiki/Application-lifetimes for details", true)]
        public static void Run(this Application app) => throw new NotSupportedException();

        /// <summary>
        /// On desktop-style platforms runs the application's main loop until closable is closed
        /// </summary>
        /// <remarks>
        /// Consider using StartWithDesktopStyleLifetime instead, see https://github.com/AvaloniaUI/Avalonia/wiki/Application-lifetimes for details
        /// </remarks>
        public static void Run(this Application app, ICloseable closable)
        {
            var cts = new CancellationTokenSource();
            closable.Closed += (s, e) => cts.Cancel();

            app.Run(cts.Token);
        }

        /// <summary>
        /// On desktop-style platforms runs the application's main loop until main window is closed
        /// </summary>
        /// <remarks>
        /// Consider using StartWithDesktopStyleLifetime instead, see https://github.com/AvaloniaUI/Avalonia/wiki/Application-lifetimes for details
        /// </remarks>
        public static void Run(this Application app, Window mainWindow)
        {
            if (mainWindow == null)
            {
                throw new ArgumentNullException(nameof(mainWindow));
            }
            var cts = new CancellationTokenSource();
            mainWindow.Closed += (_, __) => cts.Cancel();
            if (!mainWindow.IsVisible)
            {
                mainWindow.Show();
            }
            app.Run(cts.Token);
        }
        
        /// <summary>
        /// On desktop-style platforms runs the application's main loop with custom CancellationToken
        /// without setting a lifetime.
        /// </summary>
        /// <param name="token">The token to track.</param>
        public static void Run(this Application app, CancellationToken token)
        {
            Dispatcher.UIThread.MainLoop(token);
        }

        public static void RunWithMainWindow<TWindow>(this Application app)
            where TWindow : Avalonia.Controls.Window, new()
        {
            var window = new TWindow();
            window.Show();
            app.Run(window);
        }
    }
}
