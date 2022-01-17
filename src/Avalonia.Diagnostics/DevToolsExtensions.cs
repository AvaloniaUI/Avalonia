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
        public static void AttachDevTools(this TopLevel root)
        {
            DevTools.Attach(root, new DevToolsOptions());
        }

        /// <summary>
        /// Attaches DevTools to a window, to be opened with the specified key gesture.
        /// </summary>
        /// <param name="root">The window to attach DevTools to.</param>
        /// <param name="gesture">The key gesture to open DevTools.</param>
        public static void AttachDevTools(this TopLevel root, KeyGesture gesture)
        {
            DevTools.Attach(root, gesture);
        }

        /// <summary>
        /// Attaches DevTools to a window, to be opened with the specified options.
        /// </summary>
        /// <param name="root">The window to attach DevTools to.</param>
        /// <param name="options">Additional settings of DevTools.</param>
        public static void AttachDevTools(this TopLevel root, DevToolsOptions options)
        {
            DevTools.Attach(root, options);
        }

        /// <summary>
        /// Attaches DevTools to a Application, to be opened with the specified options.
        /// </summary>
        /// <param name="application">The Application to attach DevTools to.</param>
        public static void AttachDevTools(this Application application)
        {
            DevTools.Attach(application, new DevToolsOptions());
        }

        /// <summary>
        /// Attaches DevTools to a Application, to be opened with the specified options.
        /// </summary>
        /// <param name="application">The Application to attach DevTools to.</param>
        /// <param name="options">Additional settings of DevTools.</param>
        /// <remarks>
        /// Attach DevTools should only be called after application initialization is complete. A good point is <see cref="Application.OnFrameworkInitializationCompleted"/>
        /// </remarks>
        /// <example>
        /// <code>
        /// public class App : Application
        /// {
        ///    public override void OnFrameworkInitializationCompleted()
        ///    {
        ///       if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        ///       {
        ///          desktopLifetime.MainWindow = new MainWindow();
        ///       }
        ///       else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewLifetime)
        ///          singleViewLifetime.MainView = new MainView();
        ///          
        ///       base.OnFrameworkInitializationCompleted();
        ///       this.AttachDevTools(new Avalonia.Diagnostics.DevToolsOptions()
        ///           {
        ///              StartupScreenIndex = 1,
        ///           });
        ///    }
        /// }
        /// </code>
        /// </example>
        public static void AttachDevTools(this Application application, DevToolsOptions options)
        {
            DevTools.Attach(application, options);
        }
    }
}
