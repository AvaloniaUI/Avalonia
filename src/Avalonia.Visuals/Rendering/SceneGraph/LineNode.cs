// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;

namespace Avalonia.Rendering.SceneGraph
{
    public class LineNode : IDrawNode
    {
        public LineNode(Matrix transform, Pen pen, Point p1, Point p2)
        {
            Transform = transform;
            Pen = pen;
            P1 = p1;
            P2 = p2;
        }

        public Matrix Transform { get; }
        public Pen Pen { get; }
        public Point P1 { get; }
        public Point P2 { get; }

        public bool Equals(Matrix transform, Pen pen, Point p1, Point p2)
        {
            return transform == Transform && pen == Pen && p1 == P1 && p2 == P2;
        }

        public void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawLine(Pen, P1, P2);
        }

        public bool HitTest(Point p)
        {
            // TODO: Implement line hit testing.
            return false;
        }
    }
}
