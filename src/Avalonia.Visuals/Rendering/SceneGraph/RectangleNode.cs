// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;

namespace Avalonia.Rendering.SceneGraph
{
    public class RectangleNode : IGeometryNode
    {
        public RectangleNode(Matrix transform, IBrush brush, Pen pen, Rect rect, float cornerRadius)
        {
            Bounds = (rect * transform).Inflate(pen?.Thickness ?? 0);
            Transform = transform;
            Brush = brush;
            Pen = pen;
            Rect = rect;
            CornerRadius = cornerRadius;
        }

        public Rect Bounds { get; }
        public Matrix Transform { get; }
        public IBrush Brush { get; }
        public Pen Pen { get; }
        public Rect Rect { get; }
        public float CornerRadius { get; }

        public bool Equals(Matrix transform, IBrush brush, Pen pen, Rect rect, float cornerRadius)
        {
            return transform == Transform &&
                Equals(brush, Brush) &&
                pen == Pen &&
                rect == Rect &&
                cornerRadius == CornerRadius;
        }

        public void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;

            if (Brush != null)
            {
                context.FillRectangle(Brush, Rect, CornerRadius);
            }

            if (Pen != null)
            {
                context.DrawRectangle(Pen, Rect, CornerRadius);
            }
        }

        public bool HitTest(Point p) => Bounds.Contains(p);
    }
}
