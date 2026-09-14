using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RenderDemo.Controls
{
    public sealed class ResizePattern : Control
    {
        private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.FromRgb(60, 90, 130)), 1);
        private static readonly IPen DiagonalPen = new Pen(new SolidColorBrush(Color.FromRgb(90, 90, 90)), 1);
        private static readonly IPen CirclePen = new Pen(Brushes.OrangeRed, 4);
        private static readonly IPen EdgePen = new Pen(Brushes.Lime, 6);
        private static readonly IBrush Background = new SolidColorBrush(Color.FromRgb(16, 16, 24));
        private static readonly IBrush TextBrush = Brushes.White;

        private const double GridStep = 40;

        static ResizePattern()
        {
            AffectsRender<ResizePattern>(BoundsProperty);
        }

        public override void Render(DrawingContext context)
        {
            var w = Bounds.Width;
            var h = Bounds.Height;
            if (w <= 0 || h <= 0)
                return;

            var rect = new Rect(0, 0, w, h);
            context.FillRectangle(Background, rect);

            // Grid -- uneven spacing on the far edges is the tell-tale of stretching.
            for (double x = 0; x <= w; x += GridStep)
                context.DrawLine(GridPen, new Point(x, 0), new Point(x, h));
            for (double y = 0; y <= h; y += GridStep)
                context.DrawLine(GridPen, new Point(0, y), new Point(w, y));

            // Corner-to-corner diagonals -- they only meet exactly at the centre
            // when the aspect ratio is correct.
            context.DrawLine(DiagonalPen, new Point(0, 0), new Point(w, h));
            context.DrawLine(DiagonalPen, new Point(w, 0), new Point(0, h));

            // Edge frame -- a stretched drawable makes this peel away from the window edge.
            context.DrawRectangle(null, EdgePen, rect.Deflate(3));

            // Concentric perfect circles centred in the window.
            var center = new Point(w / 2, h / 2);
            var maxRadius = Math.Max(10, Math.Min(w, h) / 2 - 16);
            for (var i = 1; i <= 3; i++)
            {
                var r = maxRadius * i / 3;
                context.DrawEllipse(null, CirclePen, center, r, r);
            }

            // Centre crosshair.
            context.DrawLine(CirclePen, new Point(center.X - 20, center.Y), new Point(center.X + 20, center.Y));
            context.DrawLine(CirclePen, new Point(center.X, center.Y - 20), new Point(center.X, center.Y + 20));

            // Live size read-out.
            var text = new FormattedText(
                string.Create(CultureInfo.InvariantCulture, $"{w:0} x {h:0} DIP"),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                22,
                TextBrush);
            context.DrawText(text, new Point(12, 8));
        }
    }
}
