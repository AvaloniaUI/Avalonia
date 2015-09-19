// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Cairo;
using Perspex.Cairo.Media.Imaging;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Cairo.Media
{
    internal static class TileBrushes
    {
        public static SurfacePattern CreateImageBrush(ImageBrush brush, Size targetSize)
        {
            if (brush.Source == null)
            {
                return null;
            }

            // TODO: This is directly ported from Direct2D and could probably be made more
            // efficient on cairo by taking advantage of the fact that cairo has Extend.None.
            var image = ((BitmapImpl)brush.Source.PlatformImpl).Surface;
            var imageSize = new Size(brush.Source.PixelWidth, brush.Source.PixelHeight);
            var tileMode = brush.TileMode;
            var sourceRect = brush.SourceRect.ToPixels(imageSize);
            var destinationRect = brush.DestinationRect.ToPixels(targetSize);
            var scale = brush.Stretch.CalculateScaling(destinationRect.Size, sourceRect.Size);
            var translate = CalculateTranslate(brush, sourceRect, destinationRect, scale);
            var intermediateSize = CalculateIntermediateSize(tileMode, targetSize, destinationRect.Size);

            using (var intermediate = new ImageSurface(Format.ARGB32, (int)intermediateSize.Width, (int)intermediateSize.Height))
            using (var context = new Context(intermediate))
            {
                Rect drawRect;
                var transform = CalculateIntermediateTransform(
                    tileMode,
                    sourceRect,
                    destinationRect,
                    scale,
                    translate,
                    out drawRect);
                context.Rectangle(drawRect.ToCairo());
                context.Clip();
                context.Transform(transform.ToCairo());
                Gdk.CairoHelper.SetSourcePixbuf(context, image, 0, 0);
                context.Rectangle(0, 0, imageSize.Width, imageSize.Height);
                context.Fill();

                var result = new SurfacePattern(intermediate);

                if ((brush.TileMode & TileMode.FlipXY) != 0)
                {
                    // TODO: Currently always FlipXY as that's all cairo supports natively. 
                    // Support separate FlipX and FlipY by drawing flipped images to intermediate
                    // surface.
                    result.Extend = Extend.Reflect;
                }
                else
                {
                    result.Extend = Extend.Repeat;
                }

                if (brush.TileMode != TileMode.None)
                {
                    var matrix = result.Matrix;
                    matrix.InitTranslate(-destinationRect.X, -destinationRect.Y);
                    result.Matrix = matrix;
                }

                return result;
            }
        }

        public static SurfacePattern CreateVisualBrush(VisualBrush brush, Size targetSize)
        {
            var visual = brush.Visual;

            if (visual == null)
            {
                return null;
            }

            var layoutable = visual as ILayoutable;

            if (layoutable?.IsArrangeValid == false)
            {
                layoutable.Measure(Size.Infinity);
                layoutable.Arrange(new Rect(layoutable.DesiredSize));
            }

            // TODO: This is directly ported from Direct2D and could probably be made more
            // efficient on cairo by taking advantage of the fact that cairo has Extend.None.
            var tileMode = brush.TileMode;
            var sourceRect = brush.SourceRect.ToPixels(layoutable.Bounds.Size);
            var destinationRect = brush.DestinationRect.ToPixels(targetSize);
            var scale = brush.Stretch.CalculateScaling(destinationRect.Size, sourceRect.Size);
            var translate = CalculateTranslate(brush, sourceRect, destinationRect, scale);
            var intermediateSize = CalculateIntermediateSize(tileMode, targetSize, destinationRect.Size);
            
			using (var intermediate = new ImageSurface(Format.ARGB32, (int)intermediateSize.Width, (int)intermediateSize.Height))
            using (var context = new Context(intermediate))
            {
                Rect drawRect;
                var transform = CalculateIntermediateTransform(
                    tileMode,
                    sourceRect,
                    destinationRect,
                    scale,
                    translate,
                    out drawRect);
                var renderer = new Renderer(intermediate);

                context.Rectangle(drawRect.ToCairo());
                context.Clip();
                context.Transform(transform.ToCairo());
                renderer.Render(visual, new PlatformHandle(IntPtr.Zero, "RTB"), transform, drawRect);

                var result = new SurfacePattern(intermediate);

                if ((brush.TileMode & TileMode.FlipXY) != 0)
                {
                    // TODO: Currently always FlipXY as that's all cairo supports natively. 
                    // Support separate FlipX and FlipY by drawing flipped images to intermediate
                    // surface.
                    result.Extend = Extend.Reflect;
                }
                else
                {
                    result.Extend = Extend.Repeat;
                }

                if (brush.TileMode != TileMode.None)
                {
                    var matrix = result.Matrix;
                    matrix.InitTranslate(-destinationRect.X, -destinationRect.Y);
                    result.Matrix = matrix;
                }

                return result;
            }
        }

        /// <summary>
        /// Calculates a translate based on a <see cref="TileBrush"/>, a source and destination
        /// rectangle and a scale.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="sourceRect">The source rectangle.</param>
        /// <param name="destinationRect">The destination rectangle.</param>
        /// <param name="scale">The scale factor.</param>
        /// <returns>A vector with the X and Y translate.</returns>
        private static Vector CalculateTranslate(
            TileBrush brush,
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

        private static Size CalculateIntermediateSize(
            TileMode tileMode,
            Size targetSize,
            Size destinationSize)
        {
            var result = tileMode == TileMode.None ? targetSize : destinationSize;
            return result;
        }

        private static Matrix CalculateIntermediateTransform(
            TileMode tileMode,
            Rect sourceRect,
            Rect destinationRect,
            Vector scale,
            Vector translate,
            out Rect drawRect)
        {
            var transform = Matrix.CreateTranslation(-sourceRect.Position) *
                Matrix.CreateScale(scale) *
                Matrix.CreateTranslation(translate);
            Rect dr;

            if (tileMode == TileMode.None)
            {
                dr = destinationRect;
                transform *= Matrix.CreateTranslation(destinationRect.Position);
            }
            else
            {
                dr = new Rect(destinationRect.Size);
            }

            drawRect = dr;

            return transform;
        }
    }
}
