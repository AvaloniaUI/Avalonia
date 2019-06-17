// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Rendering;
using JetBrains.Annotations;

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
        /// Gets the client size of the toplevel.
        /// </summary>
        Size ClientSize { get; }

        /// <summary>
        /// Gets the scaling factor for the toplevel.
        /// </summary>
        double Scaling { get; }

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
        /// Gets or sets a method called when the toplevel receives input.
        /// </summary>
        Action<RawInputEventArgs> Input { get; set; }

        /// <summary>
        /// Gets or sets a method called when the toplevel requires painting.
        /// </summary>
        Action<Rect> Paint { get; set; }

        /// <summary>
        /// Gets or sets a method called when the toplevel is resized.
        /// </summary>
        Action<Size> Resized { get; set; }

        /// <summary>
        /// Gets or sets a method called when the toplevel's scaling changes.
        /// </summary>
        Action<double> ScalingChanged { get; set; }

        /// <summary>
        /// Creates a new renderer for the toplevel.
        /// </summary>
        /// <param name="root">The toplevel.</param>
        IRenderer CreateRenderer(IRenderRoot root);

        /// <summary>
        /// Invalidates a rect on the toplevel.
        /// </summary>
        void Invalidate(Rect rect);

        /// <summary>
        /// Sets the <see cref="IInputRoot"/> for the toplevel.
        /// </summary>
        void SetInputRoot(IInputRoot inputRoot);

        /// <summary>
        /// Converts a point from screen to client coordinates.
        /// </summary>
        /// <param name="point">The point in screen coordinates.</param>
        /// <returns>The point in client coordinates.</returns>
        Point PointToClient(PixelPoint point);

        /// <summary>
        /// Converts a point from client to screen coordinates.
        /// </summary>
        /// <param name="point">The point in client coordinates.</param>
        /// <returns>The point in screen coordinates.</returns>
        PixelPoint PointToScreen(Point point);

        /// <summary>
        /// Sets the cursor associated with the toplevel.
        /// </summary>
        /// <param name="cursor">The cursor. Use null for default cursor</param>
        void SetCursor(IPlatformHandle cursor);

        /// <summary>
        /// Gets or sets a method called when the underlying implementation is destroyed.
        /// </summary>
        Action Closed { get; set; }

        /// <summary>
        /// Gets a mouse device associated with toplevel
        /// </summary>
        [CanBeNull]
        IMouseDevice MouseDevice { get; }
        
        [CanBeNull]
        IKeyboardDevice KeyboardDevice { get; }
    }
}
