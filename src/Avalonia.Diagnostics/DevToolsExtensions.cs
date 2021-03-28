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
    }
}
