using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    internal abstract class BrushDrawOperation : IDrawOperation
    {
        public abstract Rect Bounds { get; }
        public abstract bool HitTest(Point p);
        public abstract IDictionary<IVisual, Scene> ChildScenes { get; }

        public abstract void Render(IDrawingContextImpl context);

        protected IBrush ToImmutable(IBrush brush)
        {
            return (brush as IMutableBrush)?.ToImmutable() ?? brush;
        }

        protected Pen ToImmutable(Pen pen)
        {
            var brush = pen?.Brush != null ? ToImmutable(pen.Brush) : null;
            return ReferenceEquals(pen?.Brush, brush) ?
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
