using System;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Automation.Peers
{
    public class ProgressBarAutomationPeer : RangeBaseAutomationPeer, IRangeValueProvider
    {
        public ProgressBarAutomationPeer(RangeBase owner) : base(owner)
        {
        }

        protected override string GetClassNameCore()
        {
            return "ProgressBar";
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ProgressBar;
        }

        /// <summary>
        /// Request to set the value that this UI element is representing
        /// </summary>
        /// <param name="val">Value to set the UI to, as an object</param>
        /// <returns>true if the UI element was successfully set to the specified value</returns>
        void IRangeValueProvider.SetValue(double val)
        {
            throw new InvalidOperationException("ProgressBar is ReadOnly, value can't be set.");
        }

        ///<summary>Indicates that the value can only be read, not modified.
        ///returns True if the control is read-only</summary>
        bool IRangeValueProvider.IsReadOnly
        {
            get => true;
        }

        ///<summary>Value of a Large Change</summary>
        double IRangeValueProvider.LargeChange
        {
            get => double.NaN;
        }

        ///<summary>Value of a Small Change</summary>
        double IRangeValueProvider.SmallChange
        {
            get => double.NaN;
        }
    }
}
