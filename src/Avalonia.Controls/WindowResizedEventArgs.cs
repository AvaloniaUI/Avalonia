using System;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// Describes the reason for a <see cref="WindowBase.Resized"/> event.
    /// </summary>
    public enum WindowResizeReason
    {
        /// <summary>
        /// The resize reason is unknown or unspecified.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The resize was due to the user resizing the window, for example by dragging the
        /// window frame.
        /// </summary>
        User,

        /// <summary>
        /// The resize was initiated by the application, for example by setting one of the sizing-
        /// related properties on <see cref="Window"/> such as <see cref="Layoutable.Width"/> or
        /// <see cref="Layoutable.Height"/>.
        /// </summary>
        Application,

        /// <summary>
        /// The resize was initiated by the layout system.
        /// </summary>
        Layout,

        /// <summary>
        /// The resize was due to a change in DPI.
        /// </summary>
        DpiChange,
    }

    /// <summary>
    /// Provides data for the <see cref="WindowBase.Resized"/> event.
    /// </summary>
    public class WindowResizedEventArgs : EventArgs
    {
        internal WindowResizedEventArgs(Size clientSize, WindowResizeReason reason)
        {
            ClientSize = clientSize;
            Reason = reason;
        }

        /// <summary>
        /// Gets the new client size of the window in device-independent pixels.
        /// </summary>
        public Size ClientSize { get; }

        /// <summary>
        /// Gets the reason for the resize.
        /// </summary>
        public WindowResizeReason Reason { get; }
    }
}
