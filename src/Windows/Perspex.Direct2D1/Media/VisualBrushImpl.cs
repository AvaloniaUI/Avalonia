// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Rendering;
using SharpDX.Direct2D1;

namespace Perspex.Direct2D1.Media
{
    public class VisualBrushImpl : TileBrushImpl
    {
        public VisualBrushImpl(
            VisualBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            Size targetSize)
        {
            var visual = brush.Visual;

            if (visual == null)
            {
                return;
            }

            var layoutable = visual as ILayoutable;

            if (layoutable?.IsArrangeValid == false)
            {
                layoutable.Measure(Size.Infinity);
                layoutable.Arrange(new Rect(layoutable.DesiredSize));
            }

            var tileMode = brush.TileMode;
            var sourceRect = brush.SourceRect.ToPixels(layoutable.Bounds.Size);
            var destinationRect = brush.DestinationRect.ToPixels(targetSize);
            var scale = brush.Stretch.CalculateScaling(destinationRect.Size, sourceRect.Size);
            var translate = CalculateTranslate(brush, sourceRect, destinationRect, scale);
            var intermediateSize = CalculateIntermediateSize(tileMode, targetSize, destinationRect.Size);
            var brtOpts = CompatibleRenderTargetOptions.None;

            // TODO: There are times where we don't need to draw an intermediate bitmap. Identify
            // them and directly use 'image' in those cases.
            using (var intermediate = new BitmapRenderTarget(target, brtOpts, intermediateSize))
            {
                Rect drawRect;
                var transform = CalculateIntermediateTransform(
                    tileMode,
                    sourceRect,
                    destinationRect,
                    scale,
                    translate,
                    out drawRect);
                var renderer = new RenderTarget(intermediate);

                using (var ctx = renderer.CreateDrawingContext())
                using (ctx.PushClip(drawRect))
                using (ctx.PushTransform(transform))
                {
                    ctx.Render(visual);
                }
                this.PlatformBrush = new BitmapBrush(
                    target,
                    intermediate.Bitmap,
                    GetBitmapBrushProperties(brush),
                    GetBrushProperties(brush, destinationRect));
            }

            //var sourceRect = brush.SourceRect.ToPixels(layoutable.Bounds.Size);
            //var destinationRect = brush.DestinationRect.ToPixels(targetSize);
            //var bitmapSize = brush.TileMode == TileMode.None ? targetSize : destinationRect.Size;
            //var scale = brush.Stretch.CalculateScaling(destinationRect.Size, sourceRect.Size);
            //var translate = CalculateTranslate(brush, sourceRect, destinationRect, scale);
            //var options = CompatibleRenderTargetOptions.None;

            //using (var brt = new BitmapRenderTarget(target, options, bitmapSize.ToSharpDX()))
            //{
            //    var renderer = new Renderer(brt);
            //    var transform = Matrix.CreateTranslation(-sourceRect.Position) *
            //                    Matrix.CreateScale(scale) *
            //                    Matrix.CreateTranslation(translate);

            //    Rect drawRect;

            //    if (brush.TileMode == TileMode.None)
            //    {
            //        drawRect = destinationRect;
            //        transform *= Matrix.CreateTranslation(destinationRect.Position);
            //    }
            //    else
            //    {
            //        drawRect = new Rect(0, 0, destinationRect.Width, destinationRect.Height);
            //    }

            //    renderer.Render(visual, null, transform, drawRect);

            //    var result = new BitmapBrush(brt, brt.Bitmap);
            //    result.ExtendModeX = GetExtendModeX(brush.TileMode);
            //    result.ExtendModeY = GetExtendModeY(brush.TileMode);

            //    if (brush.TileMode != TileMode.None)
            //    {
            //        result.Transform = SharpDX.Matrix3x2.Translation(
            //            (float)destinationRect.X,
            //            (float)destinationRect.Y);
            //    }

            //    PlatformBrush = result;
            //}
        }
    }
}
