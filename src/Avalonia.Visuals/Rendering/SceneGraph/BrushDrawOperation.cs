// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Base class for draw operations that can use a brush.
    /// </summary>
    internal abstract class BrushDrawOperation : IDrawOperation
    {
        /// <inheritdoc/>
        public abstract Rect Bounds { get; }

        /// <inheritdoc/>
        public abstract bool HitTest(Point p);

        /// <summary>
        /// Gets a collection of child scenes that are needed to draw visual brushes.
        /// </summary>
        public abstract IDictionary<IVisual, Scene> ChildScenes { get; }

        /// <inheritdoc/>
        public abstract void Render(IDrawingContextImpl context);

        /// <summary>
        /// Converts a possibly mutable brush to an immutable brush.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <returns>An immutable brush</returns>
        protected IBrush ToImmutable(IBrush brush)
        {
            return (brush as IMutableBrush)?.ToImmutable() ?? brush;
        }

        /// <summary>
        /// Converts pen with a possibly mutable brush to a pen with an immutable brush.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <returns>A pen with an immutable brush</returns>
        protected Pen ToImmutable(Pen pen)
        {
            var brush = pen?.Brush != null ? ToImmutable(pen.Brush) : null;
            return pen == null || ReferenceEquals(pen?.Brush, brush) ?
                pen :
                new Pen(
                    brush,
                    thickness: pen.Thickness,
                    dashStyle: pen.DashStyle,
                    dashCap: pen.DashCap,
                    startLineCap: pen.StartLineCap,
                    endLineCap: pen.EndLineCap,
                    lineJoin: pen.LineJoin,
                    miterLimit: pen.MiterLimit);
        }
    }
}
