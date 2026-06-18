using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TextTestApp
{
    public class SelectionAdorner : Control
    {
        public static readonly StyledProperty<IBrush?> FillProperty =
            AvaloniaProperty.Register<SelectionAdorner, IBrush?>(nameof(Fill));

        public static readonly StyledProperty<IBrush?> StrokeProperty =
            AvaloniaProperty.Register<SelectionAdorner, IBrush?>(nameof(Stroke));

        public static readonly StyledProperty<Matrix> TransformProperty =
            AvaloniaProperty.Register<SelectionAdorner, Matrix>(nameof(Transform), Matrix.Identity);

        public Matrix Transform
        {
            get => this.GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        public IBrush? Stroke
        {
            get => GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public IBrush? Fill
        {
            get => GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        private IList<Rect>? _rectangles;
        public IList<Rect>? Rectangles
        {
            get => _rectangles;
            set
            {
                _rectangles = value;
                InvalidateVisual();
            }
        }

        public SelectionAdorner()
        {
            AffectsRender<SelectionAdorner>(FillProperty, StrokeProperty, TransformProperty);
        }

        public override void Render(DrawingContext context)
        {
            var rectangles = Rectangles;
            if (rectangles == null)
                return;

            using (context.PushTransform(Transform))
            {
                Pen pen = new Pen(Stroke, 1);
                for (int i = 0; i < rectangles.Count; i++)
                {
                    Rect rectangle = rectangles[i];
                    Rect normalized = rectangle.Width < 0 ? new Rect(rectangle.TopRight, rectangle.BottomLeft) : rectangle;

                    if (rectangles[i].Width == 0)
                        context.DrawLine(pen, rectangle.TopLeft, rectangle.BottomRight);
                    else
                        context.DrawRectangle(Fill, pen, normalized);

                    RenderCue(context, pen, rectangle.TopLeft, 5, isFilled: true);
                    RenderCue(context, pen, rectangle.TopRight, 5, isFilled: false);
                }
            }
        }

        private void RenderCue(DrawingContext context, IPen pen, Point p, double size, bool isFilled)
        {
            context.DrawGeometry(pen.Brush, pen, new PolylineGeometry(
            [
                new Point(p.X - size / 2, p.Y - size),
                new Point(p.X + size / 2, p.Y - size),
                new Point(p.X, p.Y),
                new Point(p.X - size / 2, p.Y - size),
            ], isFilled));
        }
    }
}
