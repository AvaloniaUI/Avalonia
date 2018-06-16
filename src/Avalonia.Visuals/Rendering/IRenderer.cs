// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.VisualTree;
using System.Collections.Generic;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines the interface for a renderer.
    /// </summary>
    public interface IRenderer : IDisposable
    {
        /// <summary>
        /// Gets or sets a value indicating whether the renderer should draw an FPS counter.
        /// </summary>
        bool DrawFps { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the renderer should a visual representation
        /// of its dirty rectangles.
        /// </summary>
        bool DrawDirtyRects { get; set; }

        /// <summary>
        /// Mark a visual as dirty and needing re-rendering.
        /// </summary>
        /// <param name="visual">The visual.</param>
        void AddDirty(IVisual visual);

        /// <summary>
        /// Hit tests a location to find the visuals at the specified point.
        /// </summary>
        /// <param name="p">The point, in client coordinates.</param>
        /// <param name="root">The root of the subtree to search.</param>
        /// <param name="filter">
        /// A filter predicate. If the predicate returns false then the visual and all its
        /// children will be excluded from the results.
        /// </param>
        /// <returns>The visuals at the specified point, topmost first.</returns>
        IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter);

        /// <summary>
        /// Called when a resize notification is received by the control being rendered.
        /// </summary>
        /// <param name="size">The new size of the window.</param>
        void Resized(Size size);

        /// <summary>
        /// Called when a paint notification is received by the control being rendered.
        /// </summary>
        /// <param name="rect">The dirty rectangle.</param>
        void Paint(Rect rect);

        /// <summary>
        /// Starts the renderer.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the renderer.
        /// </summary>
        void Stop();
    }
}