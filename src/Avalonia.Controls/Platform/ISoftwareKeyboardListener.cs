using System;
using Avalonia.Metadata;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Listener for the platform's software keyboard. Provides access to the software keyboard height and state.
    /// </summary>
    [Unstable]
    [NotClientImplementable]
    public interface ISoftwareKeyboardListener
    {
        /// <summary>
        /// The current software keyboard state
        /// </summary>
        SoftwareKeyboardState SoftwareKeyboardState { get; }

        /// <summary>
        /// The current software keyboard frame rect.
        /// </summary>
        Rect SoftwareKeyboardRect { get; }

        /// <summary>
        /// Occurs when the software keyboard's state has changed.
        /// </summary>
        event EventHandler<SoftwareKeyboardStateChangedEventArgs> SoftwareKeyboardStateChanged;

        /// <summary>
        /// Occurs when the software keyboard's height has changed.
        /// </summary>
        event EventHandler SoftwareKeyboardHeightChanged;
    }

    /// <summary>
    /// The software keyboard opened state.
    /// </summary>
    public enum SoftwareKeyboardState
    {
        /// <summary>
        /// The software keyboard is either closed, or doesn't form part of the platform insets, i.e. it's floating or is an overlay
        /// </summary>
        Closed,

        /// <summary>
        /// The software keyboard is open.
        /// </summary>
        Open
    }

    /// <summary>
    /// Provides state change information about the software keyboard.
    /// </summary>
    public class SoftwareKeyboardStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The new state of the suftware keyboard
        /// </summary>
        public SoftwareKeyboardState NewState { get; }

        /// <summary>
        /// The previous state before the change occured.
        /// </summary>
        public SoftwareKeyboardState OldState { get; }

        public SoftwareKeyboardStateChangedEventArgs(SoftwareKeyboardState oldState, SoftwareKeyboardState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }
}
