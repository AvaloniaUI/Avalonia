// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    internal class GeometryNode : BrushDrawOperation
    {
        public GeometryNode(
            Matrix transform,
            IBrush brush,
            Pen pen,
            IGeometryImpl geometry,
            IDictionary<IVisual, Scene> childScenes = null)
        {
            Bounds = geometry.GetRenderBounds(pen.Thickness).TransformToAABB(transform);
            Transform = transform;
            Brush = ToImmutable(brush);
            Pen = ToImmutable(pen);
            Geometry = geometry;
            ChildScenes = childScenes;
        }

        public override Rect Bounds { get; }
        public Matrix Transform { get; }
        public IBrush Brush { get; }
        public Pen Pen { get; }
        public IGeometryImpl Geometry { get; }
        public override IDictionary<IVisual, Scene> ChildScenes { get; }

        public bool Equals(Matrix transform, IBrush brush, Pen pen, IGeometryImpl geometry)
        {
            return transform == Transform &&
                Equals(brush, Brush) && 
                pen == Pen &&
                Equals(geometry, Geometry);
        }

        public override void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawGeometry(Brush, Pen, Geometry);
        }

        public override bool HitTest(Point p)
        {
            p *= Transform.Invert();
            return (Brush != null && Geometry.FillContains(p)) || 
                (Pen != null && Geometry.StrokeContains(Pen, p));
        }
    }
}
