using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="NavigationPage.ModalPopped"/> event.
    /// </summary>
    public class ModalPoppedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModalPoppedEventArgs"/> class.
        /// </summary>
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
