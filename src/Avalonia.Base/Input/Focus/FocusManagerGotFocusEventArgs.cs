using System;

namespace Avalonia.Input
{
    /// <summary>
    /// Provides data for the FocusManager GotFocus event
    /// </summary>
    public class FocusManagerGotFocusEventArgs : EventArgs
    {
        internal FocusManagerGotFocusEventArgs(Guid id, IInputElement? newFocusedElement)
        {
            CorrelationID = id;
            NewFocusedElement = newFocusedElement;
        }

        /// <summary>
        /// Gets the unique ID generated when a focus movement event is initiated
        /// </summary>
        public Guid CorrelationID { get; }

        /// <summary>
        /// Gets the most recently focused element
        /// </summary>
        public IInputElement? NewFocusedElement { get; }
    }
}
