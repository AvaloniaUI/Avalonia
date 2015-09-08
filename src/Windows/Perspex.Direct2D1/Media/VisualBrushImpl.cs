





namespace Perspex.Direct2D1.Media
{
    using System;
    using Perspex.Layout;
    using Perspex.Media;
    using SharpDX.Direct2D1;

    public class VisualBrushImpl : BrushImpl
    {
        public VisualBrushImpl(
            Perspex.Media.VisualBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            Size targetSize)
        {
            var visual = brush.Visual;
            var layoutable = visual as ILayoutable;

            if (layoutable?.IsArrangeValid == false)
            {
                layoutable.Measure(Size.Infinity);
                layoutable.Arrange(new Rect(layoutable.DesiredSize));
            }

            var sourceRect = brush.SourceRect.ToPixels(layoutable.Bounds.Size);
            var destinationRect = brush.DestinationRect.ToPixels(targetSize);
            var bitmapSize = brush.TileMode == TileMode.None ? targetSize : destinationRect.Size;
            var scale = brush.Stretch.CalculateScaling(destinationRect.Size, sourceRect.Size);
            var translate = CalculateTranslate(brush, sourceRect, destinationRect, scale);
            var options = CompatibleRenderTargetOptions.None;

            using (var brt = new BitmapRenderTarget(target, options, bitmapSize.ToSharpDX()))
            {
                var renderer = new Renderer(brt);
                var transform = Matrix.CreateTranslation(-sourceRect.Position) *
                                Matrix.CreateScale(scale) *
                                Matrix.CreateTranslation(translate);

                Rect drawRect;

                if (brush.TileMode == TileMode.None)
                {
                    drawRect = destinationRect;
                    transform *= Matrix.CreateTranslation(destinationRect.Position);
                }
                else
                {
                    drawRect = new Rect(0, 0, destinationRect.Width, destinationRect.Height);
                }

                renderer.Render(visual, null, transform, drawRect);

                var result = new BitmapBrush(brt, brt.Bitmap);
                result.ExtendModeX = (brush.TileMode & TileMode.FlipX) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
                result.ExtendModeY = (brush.TileMode & TileMode.FlipY) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;

                if (brush.TileMode != TileMode.None)
                {
                    result.Transform = SharpDX.Matrix3x2.Translation(
                        (float)destinationRect.X,
                        (float)destinationRect.Y);
                }

                this.PlatformBrush = result;
            }
        }

        private static Vector CalculateTranslate(
            VisualBrush brush,
            Rect sourceRect,
            Rect destinationRect,
            Vector scale)
        {
            var x = 0.0;
            var y = 0.0;
            var size = sourceRect.Size * scale;

            switch (brush.AlignmentX)
            {
                case AlignmentX.Center:
                    x += (destinationRect.Width - size.Width) / 2;
                    break;
                case AlignmentX.Right:
                    x += destinationRect.Width - size.Width;
                    break;
            }

            switch (brush.AlignmentY)
            {
                case AlignmentY.Center:
                    y += (destinationRect.Height - size.Height) / 2;
                    break;
                case AlignmentY.Bottom:
                    y += destinationRect.Height - size.Height;
                    break;
            }

            return new Vector(x, y);
        }

        public override void Dispose()
        {
            ((BitmapBrush)this.PlatformBrush).Bitmap.Dispose();
            base.Dispose();
        }
    }
}
