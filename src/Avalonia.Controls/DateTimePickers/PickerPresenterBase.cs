using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Defines the base class for Date and Time PickerPresenters
    /// </summary>
    public abstract class PickerPresenterBase : TemplatedControl
    {
        protected virtual void OnConfirmed()
        {
            Confirmed?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDismiss()
        {
            Dismissed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Confirmed;
        public event EventHandler? Dismissed;
    }
}
