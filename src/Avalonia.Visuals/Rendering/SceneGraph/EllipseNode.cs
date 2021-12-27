﻿using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents an ellipse draw.
    /// </summary>
    internal class EllipseNode : BrushDrawOperation
    {
        public EllipseNode(
            Matrix transform, 
            IBrush? brush, 
            IPen? pen, 
            Rect rect, 
            IDictionary<IVisual, Scene>? childScenes = null) 
            : base(rect.Inflate(pen?.Thickness ?? 0), transform)
        {
            Transform = transform;
            Brush = brush?.ToImmutable();
            Pen = pen?.ToImmutable();
            Rect = rect;
            ChildScenes = childScenes;
        }

        /// <summary>
        /// Gets the fill brush.
        /// </summary>
        public IBrush? Brush { get; }

        /// <summary>
        /// Gets the stroke pen.
        /// </summary>
        public ImmutablePen? Pen { get; }

        /// <summary>
        /// Gets the transform with which the node will be drawn.
        /// </summary>
        public Matrix Transform { get; }

        /// <summary>
        /// Gets the rect of the ellipse to draw.
        /// </summary>
        public Rect Rect { get; }

        public override IDictionary<IVisual, Scene>? ChildScenes { get; }

        public bool Equals(Matrix transform, IBrush? brush, IPen? pen, Rect rect)
        {
            return transform == Transform &&
                   Equals(brush, Brush) &&
                   Equals(Pen, pen) &&
                   rect.Equals(Rect);
        }

        public override void Render(IDrawingContextImpl context)
        {
            context.DrawEllipse(Brush, Pen, Rect);
        }

        public override bool HitTest(Point p)
        {
            if (!Transform.TryInvert(out Matrix inverted))
            {
                return false;
            }

            p *= inverted;

            var center = Rect.Center;

            var strokeThickness = Pen?.Thickness ?? 0;

            var rx = Rect.Width / 2 + strokeThickness / 2;
            var ry = Rect.Height / 2 + strokeThickness / 2;

            var dx = p.X - center.X;
            var dy = p.Y - center.Y;

            if (Math.Abs(dx) > rx || Math.Abs(dy) > ry)
            {
                return false;
            }

            if (Brush != null)
            {
                return Contains(rx, ry);
            }
            else if (strokeThickness > 0)
            {
                bool inStroke = Contains(rx, ry);

                rx = Rect.Width / 2 - strokeThickness / 2;
                ry = Rect.Height / 2 - strokeThickness / 2;

                bool inInner = Contains(rx, ry);

                return inStroke && !inInner;
            }

            bool Contains(double radiusX, double radiusY)
            {
                var rx2 = radiusX * radiusX;
                var ry2 = radiusY * radiusY;

                var distance = ry2 * dx * dx + rx2 * dy * dy;

                return distance <= rx2 * ry2;
            }

            return false;
        }
    }
}
