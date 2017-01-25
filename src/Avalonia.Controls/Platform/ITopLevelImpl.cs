// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Platform
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
        /// Gets the maximum size of a window on the system.
        /// </summary>
        Size MaxClientSize { get; }

        /// <summary>
        /// Gets the scaling factor for the window.
        /// </summary>
        double Scaling { get; }

        /// <summary>
        /// Gets the platform window handle.
        /// </summary>
        IPlatformHandle Handle { get; }

        /// <summary>
        /// The list of native platform's surfaces that can be consumed by rendering subsystems.
        /// </summary>
        /// <remarks>
        /// Rendering platform will check that list and see if it can utilize one of them to output.
        /// It should be enough to expose a native window handle via IPlatformHandle
        /// and add support for framebuffer (even if it's emulated one) via IFramebufferPlatformSurface.
        /// If you have some rendering platform that's tied to your particular windowing platform,
        /// just expose some toolkit-specific object (e. g. Func&lt;Gdk.Drawable&gt; in case of GTK#+Cairo)
        /// </remarks>
        IEnumerable<object> Surfaces { get; }

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
        /// Gets or sets a method called when the window's scaling changes.
        /// </summary>
        Action<double> ScalingChanged { get; set; }

        /// <summary>
        /// Gets or sets a method called when the window's position changes.
        /// </summary>
        Action<Point> PositionChanged { get; set; }

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
        /// Converts a point from screen to client coordinates.
        /// </summary>
        /// <param name="point">The point in screen coordinates.</param>
        /// <returns>The point in client coordinates.</returns>
        Point PointToClient(Point point);

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
        /// Hides the window.
        /// </summary>
        void Hide();

        /// <summary>
        /// Starts moving a window with left button being held. Should be called from left mouse button press event handler.
        /// </summary>
        void BeginMoveDrag();

        /// <summary>
        /// Starts resizing a window. This function is used if an application has window resizing controls. 
        /// Should be called from left mouse button press event handler
        /// </summary>
        void BeginResizeDrag(WindowEdge edge);

        Point Position { get; set; }
    }
}
