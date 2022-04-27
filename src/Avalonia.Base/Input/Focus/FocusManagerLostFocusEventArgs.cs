using System;

namespace Avalonia.Input
{
    /// <summary>
    /// Provides data for the FocusManager LostFocus event
    /// </summary>
    public class FocusManagerLostFocusEventArgs : EventArgs
    {
        internal FocusManagerLostFocusEventArgs(Guid id, IInputElement? oldFocusedElement)
        {
            CorrelationID = id;
            OldFocusedElement = oldFocusedElement;
        }

        /// <summary>
        /// Gets the unique ID generated when a focus movement event is initiated
        /// </summary>
        public Guid CorrelationID { get; }

        /// <summary>
        /// Gets the last focused element
        /// </summary>
        public IInputElement? OldFocusedElement { get; }
    }
}
