using System;

namespace Avalonia.Automation
{
    /// <summary>
    /// Describes a text selection or caret change raised through
    /// <see cref="Peers.AutomationPeer.RaiseTextSelectionChangedEvent"/> so automation backends can
    /// notify clients (UIA Text_TextSelectionChanged, AT-SPI text-caret-moved / text-selection-changed).
    /// </summary>
    public class AutomationTextSelectionChangedEventArgs : EventArgs
    {
        public AutomationTextSelectionChangedEventArgs(int selectionStart, int selectionEnd, int caretOffset)
        {
            SelectionStart = selectionStart;
            SelectionEnd = selectionEnd;
            CaretOffset = caretOffset;
        }

        /// <summary>The normalized selection start, in UTF-16 code units.</summary>
        public int SelectionStart { get; }

        /// <summary>The normalized selection end, in UTF-16 code units.</summary>
        public int SelectionEnd { get; }

        /// <summary>The caret offset (the selection's active end), in UTF-16 code units.</summary>
        public int CaretOffset { get; }
    }
}
