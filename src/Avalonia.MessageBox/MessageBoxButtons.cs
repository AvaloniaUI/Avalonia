using System;

namespace Avalonia.MessageBox
{
    /// <summary>
    /// Message Box Buttons
    /// </summary>
    [Flags]
    public enum MessageBoxButton
    {
        /// <summary>None, the user closed the message box</summary>
        None = 0,
        /// <summary>OK button</summary>
        OK = 1,
        /// <summary>Yes button</summary>
        Yes = 2,
        /// <summary>No button</summary>
        No = 4,
        /// <summary>Cancel button</summary>
        Cancel = 8,
    }
}
