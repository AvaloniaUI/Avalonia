using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides event data for the <see cref="TextBox.CuttingToClipboard"/>, <see cref="TextBox.CopyingToClipboard"/> and <see cref="TextBox.PasteFromClipboard"/> events.
    /// </summary>
    /// <remarks>
    /// If you perform any action in the handler for a clipboard event, set the Handled property to true; otherwise, the default action is performed.
    /// </remarks>
    public class TextBoxClipboardEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value that marks the event as handled. A true value for Handled prevents most handlers along the event from handling the same event again.
        /// </summary>
        public bool Handled { get; set; }
    }
}
