﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a line draw.
    /// </summary>
    internal class LineNode : BrushDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryNode"/> class.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The start point of the line.</param>
        /// <param name="p2">The end point of the line.</param>
        /// <param name="childScenes">Child scenes for drawing visual brushes.</param>
        public LineNode(
            Matrix transform,
            Pen pen,
            Point p1,
            Point p2,
            IDictionary<IVisual, Scene> childScenes = null)
            : base(new Rect(p1, p2), transform, pen)
        {
            Transform = transform;
            Pen = pen?.ToImmutable();
            P1 = p1;
            P2 = p2;
            ChildScenes = childScenes;
        }

        /// <summary>
        /// Gets the transform with which the node will be drawn.
        /// </summary>
        public Matrix Transform { get; }

        /// <summary>
        /// Gets the stroke pen.
        /// </summary>
        public Pen Pen { get; }

        /// <summary>
        /// Gets the start point of the line.
        /// </summary>
        public Point P1 { get; }

        /// <summary>
        /// Gets the end point of the line.
        /// </summary>
        public Point P2 { get; }

        /// <inheritdoc/>
        public override IDictionary<IVisual, Scene> ChildScenes { get; }

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="pen">The stroke of the other draw operation.</param>
        /// <param name="p1">The start point of the other draw operation.</param>
        /// <param name="p2">The end point of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(Matrix transform, Pen pen, Point p1, Point p2)
        {
            return transform == Transform && pen == Pen && p1 == P1 && p2 == P2;
        }

        public override void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawLine(Pen, P1, P2);
        }

        public override bool HitTest(Point p)
        {
            // TODO: Implement line hit testing.
            return false;
        }
    }
}
