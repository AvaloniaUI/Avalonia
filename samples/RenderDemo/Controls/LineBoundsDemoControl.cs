using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;

namespace RenderDemo.Controls
{
    public class LineBoundsDemoControl : Control
    {
        private double angle = Math.PI / 8;

        public static double CalculateOppSide(double angle, double hyp)
        {
            return Math.Sin(angle) * hyp;
        }

        public static double CalculateAdjSide(double angle, double hyp)
        {
            return Math.Cos(angle) * hyp;
        }

        public override void Render(DrawingContext drawingContext)
        {
            var lineLength = Math.Sqrt((100 * 100) + (100 * 100));

            var diffX = CalculateAdjSide(angle, lineLength);
            var diffY = CalculateOppSide(angle, lineLength);


            var p1 = new Point(200, 200);
            var p2 = new Point(p1.X + diffX, p1.Y + diffY);

            var pen = new Pen(Brushes.Green, 20, lineCap: PenLineCap.Square);
            var boundPen = new Pen(Brushes.Black);

            drawingContext.DrawLine(pen, p1, p2);

            drawingContext.DrawRectangle(boundPen, LineBoundsHelper.CalculateBounds(p1, p2, pen));

            angle += Math.PI / 360;
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }
    }
}
