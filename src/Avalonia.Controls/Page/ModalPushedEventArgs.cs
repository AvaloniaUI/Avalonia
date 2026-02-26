using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for modal push events.
    /// </summary>
    public class ModalPushedEventArgs : EventArgs
    {
        /// <param name="modal">The modal page that was pushed.</param>
        public ModalPushedEventArgs(Page modal)
        {
            Modal = modal;
        }

        /// <summary>
        /// Gets the modal page that was pushed.
        /// </summary>
        public Page Modal { get; }
    }
}
