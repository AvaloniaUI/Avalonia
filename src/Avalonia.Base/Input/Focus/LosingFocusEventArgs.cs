using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    /// <summary>
    /// Provides data for the <see cref="InputElement.LosingFocus"/> event
    /// </summary>
    public class LosingFocusEventArgs : RoutedEventArgs
    {
        internal LosingFocusEventArgs(bool canCancel = true, bool canRedirect = true)
        {
            RoutedEvent = InputElement.LosingFocusEvent;
            _canCancelFocusChange = canCancel;
            _canRedirectFocusChange = canRedirect;
        }

        /// <summary>
        /// Gets the new focus target
        /// </summary>
        public IInputElement? NewFocusedElement { get; internal set; }

        /// <summary>
        /// Gets or sets whether focus navigation should be cancelled
        /// </summary>
        /// <remarks>
        /// Focus changes cannot always be cancelled. Use <see cref="TryCancel"/> instead
        /// which returns whether the cancel operation was successful
        /// </remarks>
        public bool Cancel
        {
            get => _cancelled;
            set
            {
                if (value)
                {
                    TryCancel();
                }
            }
        }

        /// <summary>
        /// Gets the direction that focus moved from element to element within the app UI
        /// </summary>
        public NavigationDirection Direction { get; internal set; }

        /// <summary>
        /// Gets the input mode through which an element obtained focus
        /// </summary>
        public FocusState FocusState { get; internal set; }

        /// <summary>
        /// Gets the input device type from which input events are received
        /// </summary>
        public FocusInputDeviceKind InputDevice { get; internal set; }

        /// <summary>
        /// Gets the last focused object
        /// </summary>
        public IInputElement? OldFocusedElement { get; internal set; }

        /// <summary>
        /// Gets the unique ID generated when a focus movement event is initiated
        /// </summary>
        public Guid CorrelationID { get; internal set; }


        /// <summary>
        /// Gets the keymodifiers state when the focus event began
        /// </summary>
        ///  /// <remarks>
        /// Note that KeyModifiers will only be set if the focus event originated
        /// from a keyboard or pointer device. Programmatic focus events will always
        /// be <see cref="KeyModifiers.None"/>
        /// </remarks>
        public KeyModifiers KeyModifiers { get; internal set; }

        /// <summary>
        /// Attempts to cancel the ongoing focus action
        /// </summary>
        /// <returns>True if the focus action is cancelled; otherwise, false</returns>
        public bool TryCancel()
        {
            if (!_canCancelFocusChange)
            {
                return false;
            }

            _cancelled = true;
            return true;
        }

        /// <summary>
        /// Attemps to redirect focus to the specified element instead of the original
        /// target element
        /// </summary>
        /// <param name="element">The new target element for focus</param>
        /// <returns>True if the focus action is redirected; otherwise, false</returns>
        public bool TrySetNewFocusedElement(IInputElement? element)
        {
            if (!_canRedirectFocusChange)
            {
                return false;
            }

            NewFocusedElement = element;
            return true;
        }

        private bool _canCancelFocusChange;
        private bool _cancelled;
        private bool _canRedirectFocusChange;
    }
}
