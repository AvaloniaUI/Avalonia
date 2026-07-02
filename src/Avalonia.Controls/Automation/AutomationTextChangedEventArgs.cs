using System;

namespace Avalonia.Automation
{
    /// <summary>
    /// Describes a text content change raised through
    /// <see cref="Peers.AutomationPeer.RaiseTextChangedEvent"/> so automation backends can notify
    /// clients (UIA Text_TextChanged, AT-SPI object:text-changed). The removed and inserted text let
    /// a backend report the change without re-reading the document.
    /// </summary>
    public class AutomationTextChangedEventArgs : EventArgs
    {
        public AutomationTextChangedEventArgs(int offset, string removedText, string insertedText)
        {
            Offset = offset;
            RemovedText = removedText ?? string.Empty;
            InsertedText = insertedText ?? string.Empty;
        }

        /// <summary>The UTF-16 code-unit offset at which the change starts.</summary>
        public int Offset { get; }

        /// <summary>The text removed at <see cref="Offset"/>; empty for a pure insertion.</summary>
        public string RemovedText { get; }

        /// <summary>The text inserted at <see cref="Offset"/>; empty for a pure deletion.</summary>
        public string InsertedText { get; }
    }
}
