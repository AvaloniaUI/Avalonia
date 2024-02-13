namespace Avalonia.Diagnostics;

static internal class Constants
{
    /// <summary>
    /// DevTools Clipoard data format
    /// </summary>
    static public class DataFormats
    {
        /// <summary>
        /// Clipboard data format for the selector. It is added for quick format recognition in IDEs
        /// </summary>
        public const string Avalonia_DevTools_Selector = nameof(Avalonia_DevTools_Selector);
        /// <summary>
        /// Clipboard data format for selector. It is added for quicks IDE reqcongnize format
        /// </summary>
        public const string Avalonia_DevTools_SelectorFromTemplate = nameof(Avalonia_DevTools_SelectorFromTemplate);
    }
}
