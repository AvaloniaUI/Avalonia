// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Input;
using Perspex.Input.Raw;

namespace Perspex.Platform
{
    /// <summary>
    /// Defines a platform-specific top-level window implementation.
    /// </summary>
    /// <remarks>
    /// This interface is the common interface to <see cref="IWindowImpl"/> and
    /// <see cref="IPopupImpl"/>.
    /// </remarks>
    public interface ITopLevelImpl : IDisposable
    {
        /// <summary>
        /// Gets or sets the client size of the window.
        /// </summary>
        Size ClientSize { get; set; }

        /// <summary>
        /// Gets the platform window handle.
        /// </summary>
        IPlatformHandle Handle { get; }

        /// <summary>
        /// Gets or sets a method called when the window is activated (receives focus).
        /// </summary>
        Action Activated { get; set; }

        /// <summary>
        /// Gets or sets a method called when the window is closed.
        /// </summary>
        Action Closed { get; set; }

        /// <summary>
        /// Gets or sets a method called when the window is deactivated (loses focus).
        /// </summary>
        Action Deactivated { get; set; }

        /// <summary>
        /// Gets or sets a method called when the window receives input.
        /// </summary>
        Action<RawInputEventArgs> Input { get; set; }

        /// <summary>
        /// Gets or sets a method called when the window requires painting.
        /// </summary>
        Action<Rect> Paint { get; set; }

        /// <summary>
        /// Gets or sets a method called when the window is resized.
        /// </summary>
        Action<Size> Resized { get; set; }

        /// <summary>
        /// Activates the window.
        /// </summary>
        void Activate();

        /// <summary>
        /// Invalidates a rect on the window.
        /// </summary>
        void Invalidate(Rect rect);

        /// <summary>
        /// Sets the <see cref="IInputRoot"/> for the window.
        /// </summary>
        void SetInputRoot(IInputRoot inputRoot);

        /// <summary>
        /// Converts a point from client to screen coordinates.
        /// </summary>
        /// <param name="point">The point in client coordinates.</param>
        /// <returns>The point in screen coordinates.</returns>
        Point PointToScreen(Point point);

        /// <summary>
        /// Sets the cursor associated with the window.
        /// </summary>
        /// <param name="cursor">The cursor. Use null for default cursor</param>
        void SetCursor(IPlatformHandle cursor);

        /// <summary>
        /// Shows the toplevel.
        /// </summary>
        void Show();

        /// <summary>
        /// Starts moving a window with left button being held. Should be called from left mouse button press event handler
        /// </summary>
        void BeginMoveDrag();
    }
}
