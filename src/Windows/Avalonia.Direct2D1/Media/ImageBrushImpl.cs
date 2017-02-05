// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Rendering.Utilities;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public sealed class ImageBrushImpl : BrushImpl
    {
        public ImageBrushImpl(
            ITileBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            BitmapImpl bitmap,
            Size targetSize)
        {
            var calc = new TileBrushCalculator(brush, new Size(bitmap.PixelWidth, bitmap.PixelHeight), targetSize);

            if (!calc.NeedsIntermediate)
            {
                PlatformBrush = new BitmapBrush(
                    target,
                    bitmap.GetDirect2DBitmap(target),
                    GetBitmapBrushProperties(brush),
                    GetBrushProperties(brush, calc.DestinationRect));
            }
            else
            {
                using (var intermediate = RenderIntermediate(target, bitmap, calc))
                {
                    PlatformBrush = new BitmapBrush(
                        target,
                        intermediate.Bitmap,
                        GetBitmapBrushProperties(brush),
                        GetBrushProperties(brush, calc.DestinationRect));
                }
            }
        }

        public override void Dispose()
        {
            ((BitmapBrush)PlatformBrush)?.Bitmap.Dispose();
            base.Dispose();
        }

        private static BitmapBrushProperties GetBitmapBrushProperties(ITileBrush brush)
        {
            var tileMode = brush.TileMode;

            return new BitmapBrushProperties
            {
                ExtendModeX = GetExtendModeX(tileMode),
                ExtendModeY = GetExtendModeY(tileMode),
            };
        }

        private static BrushProperties GetBrushProperties(ITileBrush brush, Rect destinationRect)
        {
            var tileTransform =
                brush.TileMode != TileMode.None ?
                Matrix.CreateTranslation(destinationRect.X, destinationRect.Y) :
                Matrix.Identity;

            return new BrushProperties
            {
                Opacity = (float)brush.Opacity,
                Transform = tileTransform.ToDirect2D(),
            };
        }

        private static ExtendMode GetExtendModeX(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipX) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        private static ExtendMode GetExtendModeY(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipY) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        private BitmapRenderTarget RenderIntermediate(
            SharpDX.Direct2D1.RenderTarget target,
            BitmapImpl bitmap,
            TileBrushCalculator calc)
        {
            var result = new BitmapRenderTarget(
                target,
                CompatibleRenderTargetOptions.None,
                calc.IntermediateSize.ToSharpDX());

            using (var context = new RenderTarget(result).CreateDrawingContext(null))
            {
                var rect = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);

                context.Clear(Colors.Transparent);
                context.PushClip(calc.IntermediateClip);
                context.Transform = calc.IntermediateTransform;
                context.DrawImage(bitmap, 1, rect, rect);
                context.PopClip();
            }

            return result;
        }
    }
}
