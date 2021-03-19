using System.Diagnostics;
using System.Drawing.Drawing2D;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using Avalonia.Visuals.Media.Imaging;

namespace RenderDemo.Pages
{
    public class PathMeasurementPage : Control
    {
        private RenderTargetBitmap _bitmap;

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _bitmap = new RenderTargetBitmap(new PixelSize(500, 500), new Vector(96, 96));
            base.OnAttachedToLogicalTree(e);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _bitmap.Dispose();
            _bitmap = null;
            base.OnDetachedFromLogicalTree(e);
        }

        readonly Stopwatch _st = Stopwatch.StartNew();


        readonly IPen strokePen = new ImmutablePen(Brushes.DarkBlue, 10d, null, PenLineCap.Round, PenLineJoin.Round);
        readonly IPen strokePen1 = new ImmutablePen(Brushes.Purple, 10d, null, PenLineCap.Round, PenLineJoin.Round);
        readonly IPen strokePen2 = new ImmutablePen(Brushes.Green, 10d, null, PenLineCap.Round, PenLineJoin.Round);
        readonly IPen strokePen3 = new ImmutablePen(Brushes.LightBlue, 10d, null, PenLineCap.Round, PenLineJoin.Round);

        public override void Render(DrawingContext context)
        {
            using (var ctxi = _bitmap.CreateDrawingContext(null))
            using (var ctx = new DrawingContext(ctxi, false))
            {
                ctxi.Clear(default);

                var x = new PathGeometry();

                using (var xsad = x.Open())
                {
                    xsad.BeginFigure(new Point(20, 20), false);
                    xsad.LineTo(new Point(400, 50));
                    xsad.LineTo(new Point(80, 100));
                    xsad.LineTo(new Point(300, 150));
                    xsad.EndFigure(false);
                }

                ctx.DrawGeometry(null, strokePen, x);


                var length = x.PlatformImpl.ContourLength;


                if (x.PlatformImpl.TryGetSegment(length * 0.05, length * 0.2, true, out var dst1))
                    ctx.DrawGeometry(null, strokePen1, (Geometry)dst1);

                if (x.PlatformImpl.TryGetSegment(length * 0.2, length * 0.8, true, out var dst2))
                    ctx.DrawGeometry(null, strokePen2, (Geometry)dst2);
                
                if (x.PlatformImpl.TryGetSegment(length * 0.8, length * 0.95, true, out var dst3))
                    ctx.DrawGeometry(null, strokePen3, (Geometry)dst3);

                /*
                 * paint.Style = SKPaintStyle.Stroke;z
				paint.StrokeWidth = 10;z
				paint.IsAntialias = true;z
				paint.StrokeCap = SKStrokeCap.Round;z
				paint.StrokeJoin = SKStrokeJoin.Round;x

				path.MoveTo(20, 20);
				path.LineTo(400, 50);
				path.LineTo(80, 100);
				path.LineTo(300, 150);

				paint.Color = SampleMedia.Colors.XamarinDarkBlue;
				canvas.DrawPath(path, paint);

				using (var measure = new SKPathMeasure(path, false))
				using (var dst = new SKPath())
				{
					var length = measure.Length;

					dst.Reset();
					measure.GetSegment(length * 0.05f, length * 0.2f, dst, true);
					paint.Color = SampleMedia.Colors.XamarinPurple;
					canvas.DrawPath(dst, paint);

					dst.Reset();
					measure.GetSegment( dst, true);
					paint.Color = SampleMedia.Colors.XamarinGreen;
					canvas.DrawPath(dst, paint);

					dst.Reset();
					measure.GetSegment(length * 0.8f, length * 0.95f, dst, true);
					paint.Color = SampleMedia.Colors.XamarinLightBlue;
					canvas.DrawPath(dst, paint);
				}
                 */
                //  
                // ctx.FillRectangle(Brushes.Fuchsia, new Rect(50, 50, 100, 100));
            }

            context.DrawImage(_bitmap,
                new Rect(0, 0, 500, 500),
                new Rect(0, 0, 500, 500));
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            base.Render(context);
        }
    }
}
