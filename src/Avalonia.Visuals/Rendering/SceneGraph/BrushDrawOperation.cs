// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Base class for draw operations that can use a brush.
    /// </summary>
    internal abstract class BrushDrawOperation : DrawOperation
    {
        public BrushDrawOperation(Rect bounds, Matrix transform, Pen pen)
            : base(bounds, transform, pen)
        {
        }

        /// <summary>
        /// Gets a collection of child scenes that are needed to draw visual brushes.
        /// </summary>
        public abstract IDictionary<IVisual, Scene> ChildScenes { get; }

        protected IImmutableBrush ToImmutable(IBrush brush, double opacity)
        {
            if (brush != null)
            {
                var immutable = brush.ToImmutable();
                return opacity == 1 ? immutable : immutable.WithOpacity(immutable.Opacity * opacity);
            }

            return null;
        }

        protected Pen ToImmutable(Pen pen, double opacity)
        {
            var brush = ToImmutable(pen?.Brush, opacity);
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
