// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    public class GeometryNode : IGeometryNode
    {
        public GeometryNode(Matrix transform, IBrush brush, Pen pen, IGeometryImpl geometry)
        {
            Transform = transform;
            Brush = brush;
            Pen = pen;
            Geometry = geometry;
        }

        public Matrix Transform { get; }
        public IBrush Brush { get; }
        public Pen Pen { get; }
        public IGeometryImpl Geometry { get; }

        public bool Equals(Matrix transform, IBrush brush, Pen pen, IGeometryImpl geometry)
        {
            return transform == Transform &&
                Equals(brush, Brush) && 
                pen == Pen &&
                Equals(geometry, Geometry);
        }

        public void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawGeometry(Brush, Pen, Geometry);
        }

        public bool HitTest(Point p)
        {
            p *= Transform.Invert();
            return (Brush != null && Geometry.FillContains(p)) || 
                (Pen != null && Geometry.StrokeContains(Pen, p));
        }
    }
}
