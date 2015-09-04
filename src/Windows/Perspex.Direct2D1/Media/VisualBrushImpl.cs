// -----------------------------------------------------------------------
// <copyright file="VisualBrushImpl.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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
            var bitmapSize = CalculateBitmapSize(brush.TileMode, destinationRect.Size, targetSize);
            var scale = brush.Stretch.CalculateScaling(destinationRect.Size, sourceRect.Size);
            var translate = CalculateTranslate(brush, sourceRect, destinationRect, scale);
            var options = CompatibleRenderTargetOptions.None;

            using (var brt = new BitmapRenderTarget(target, options, bitmapSize.ToSharpDX()))
            {
                var renderer = new Renderer(brt);
                var transform = Matrix.CreateTranslation(-sourceRect.Position) *
                                Matrix.CreateScale(scale) *
                                Matrix.CreateTranslation(translate);
                renderer.Render(visual, null, transform, destinationRect);

                var result = new BitmapBrush(brt, brt.Bitmap);
                this.PlatformBrush = result;
            }
        }

        private static Size CalculateBitmapSize(TileMode tileMode, Size size, Size targetSize)
        {
            switch (tileMode)
            {
                case TileMode.None:
                    return targetSize;
                default:
                    throw new NotImplementedException();
            }
        }

        private static Vector CalculateTranslate(
            VisualBrush brush,
            Rect sourceRect,
            Rect destinationRect,
            Vector scale)
        {
            var x = destinationRect.X;
            var y = destinationRect.Y;
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
