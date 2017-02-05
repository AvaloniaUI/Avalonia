// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    internal class RectangleNode : BrushDrawOperation
    {
        public RectangleNode(
            Matrix transform,
            IBrush brush,
            Pen pen,
            Rect rect,
            float cornerRadius,
            IDictionary<IVisual, Scene> childScenes = null)
        {
            Bounds = rect.TransformToAABB(transform).Inflate(pen?.Thickness ?? 0);
            Transform = transform;
            Brush = Convert(brush);
            Pen = Convert(pen);
            Rect = rect;
            CornerRadius = cornerRadius;
            ChildScenes = childScenes;
        }

        public override Rect Bounds { get; }
        public Matrix Transform { get; }
        public IBrush Brush { get; }
        public Pen Pen { get; }
        public Rect Rect { get; }
        public float CornerRadius { get; }
        public override IDictionary<IVisual, Scene> ChildScenes { get; }

        public bool Equals(Matrix transform, IBrush brush, Pen pen, Rect rect, float cornerRadius)
        {
            return transform == Transform &&
                Equals(brush, Brush) &&
                pen == Pen &&
                rect == Rect &&
                cornerRadius == CornerRadius;
        }

        public override void Render(IDrawingContextImpl context)
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

        public override bool HitTest(Point p) => Bounds.Contains(p);
    }
}
