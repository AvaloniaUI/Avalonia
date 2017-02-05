using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    internal abstract class BrushDrawOperation : IDrawOperation
    {
        public abstract Rect Bounds { get; }
        public abstract bool HitTest(Point p);
        public abstract IDictionary<IVisual, Scene> ChildScenes { get; }

        public abstract void Render(IDrawingContextImpl context);

        protected IBrush Convert(IBrush brush)
        {
            var imageBrush = brush as ImageBrush;
            var visualBrush = brush as VisualBrush;
            
            if (imageBrush != null)
            {
                return new SceneImageBrush(imageBrush);
            }
            else if (visualBrush != null)
            {
                return new SceneVisualBrush(visualBrush);
            }
            else
            {
                return brush;
            }
        }

        protected Pen Convert(Pen pen)
        {
            var brush = pen?.Brush != null ? Convert(pen.Brush) : null;
            return ReferenceEquals(pen?.Brush, brush) ?
                pen :
                new Pen(
                    pen.Brush,
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
