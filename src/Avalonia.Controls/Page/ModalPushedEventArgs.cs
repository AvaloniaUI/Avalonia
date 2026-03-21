using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="NavigationPage.ModalPushed"/> event.
    /// </summary>
    public class ModalPushedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModalPushedEventArgs"/> class.
        /// </summary>
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
