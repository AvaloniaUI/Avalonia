using System;
using Avalonia.Interactivity;

#nullable enable

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Holds data for the <see cref="Popup.Closed"/> event.
    /// </summary>
    public class PopupClosedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PopupClosedEventArgs"/> class.
        /// </summary>
        /// <param name="closeEvent"></param>
        public PopupClosedEventArgs(EventArgs? closeEvent)
        {
            CloseEvent = closeEvent;
        }

        /// <summary>
        /// Gets the event that closed the popup, if any.
        /// </summary>
        /// <remarks>
        /// If <see cref="Popup.StaysOpen"/> is false, then this property will hold details of the
        /// interaction that caused the popup to close if the close was caused by e.g. a pointer press
        /// outside the popup. It can be used to mark the event as handled if the event should not
        /// be propagated.
        /// </remarks>
        public EventArgs? CloseEvent { get; }
    }
}
