// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    internal class LineNode : BrushDrawOperation
    {
        public LineNode(Matrix transform, Pen pen, Point p1, Point p2)
        {
            Bounds = new Rect(P1, P2);
            Transform = transform;
            Pen = Convert(pen);
            P1 = p1;
            P2 = p2;
        }

        public override Rect Bounds { get; }
        public Matrix Transform { get; }
        public Pen Pen { get; }
        public Point P1 { get; }
        public Point P2 { get; }
        public override IDictionary<IVisual, Scene> ChildScenes => null;

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
