using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data specific to a <see cref="TextBox.PastingFromClipboard"/> event.
    /// </summary>
    // TODO13: retype PastingFromClipboardEvent and the PastingFromClipboard CLR event to use this class.
    public class PastingFromClipboardEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PastingFromClipboardEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with these event args.</param>
        /// <param name="clipboard">The clipboard being pasted from.</param>
        public PastingFromClipboardEventArgs(RoutedEvent? routedEvent, IClipboard? clipboard)
            : base(routedEvent)
        {
            Clipboard = clipboard;
        }

        /// <summary>
        /// Gets the clipboard being pasted from. This is either the system clipboard or, when pasting
        /// via middle-click on platforms supporting it, <see cref="TopLevel.PrimarySelection"/>.
        /// Null when no clipboard is available; a handler can still handle the event to provide custom
        /// paste behavior.
        /// </summary>
        public IClipboard? Clipboard { get; }
    }
}
