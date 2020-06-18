using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Defines the base class for presenters used for choosing objects, like Date, Time, etc.
    /// Sort of combines FlyoutBase and PickerFlyoutBase
    /// </summary>
    public abstract class PickerPresenterBase : TemplatedControl
    {
        /// <summary>
        /// Raised when the AcceptButton is clicked on the Picker and the selected value is changed
        /// </summary>
        protected virtual void OnConfirmed()
        {
        }

        /// <summary>
        /// Gets whether the Accept/Dismiss buttons are shown in the picker
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldShowConfirmationButtons()
        {
            return true;
        }

        /// <summary>
        /// Raised when the Popup opens
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// Raised when the Popup closes
        /// </summary>
        public event EventHandler Closed;

        protected virtual void OnOpened()
        {
            Opened?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Shows the popup for the PickerPresenter at the specified control.
        /// </summary>
        /// <param name="target"></param>
        public abstract void ShowAt(Control target);


        protected Popup _hostPopup;
    }
}
