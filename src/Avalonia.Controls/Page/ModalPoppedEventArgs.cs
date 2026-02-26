using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for modal pop events.
    /// </summary>
    public class ModalPoppedEventArgs : EventArgs
    {
        /// <param name="modal">The modal page that was popped.</param>
        public ModalPoppedEventArgs(Page modal)
        {
            Modal = modal;
        }

        /// <summary>
        /// Gets the modal page that was popped.
        /// </summary>
        public Page Modal { get; }
    }
}
