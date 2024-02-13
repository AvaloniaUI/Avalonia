namespace Avalonia.Diagnostics;

internal static  class Constants
{
    /// <summary>
    /// DevTools Clipboard data format
    /// </summary>
    static public class DataFormats
    {
        /// <summary>
        /// Clipboard data format for the selector. It is added for quick format recognition in IDEs
        /// </summary>
        public const string Avalonia_DevTools_Selector = nameof(Avalonia_DevTools_Selector);
        /// <summary>
        /// Clipboard data format for selector. It is added for quicks IDE recognize format
        /// </summary>
        public const string Avalonia_DevTools_SelectorFromTemplate = nameof(Avalonia_DevTools_SelectorFromTemplate);
    }
}
