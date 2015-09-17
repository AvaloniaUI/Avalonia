// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Media;
using SharpDX.Direct2D1;

namespace Perspex.Direct2D1.Media
{
    public class ImageBrushImpl : TileBrushImpl
    {
        public ImageBrushImpl(
            ImageBrush brush,
            RenderTarget target,
            Size targetSize)
        {
            if (brush.Source == null)
            {
                return;
            }

            var image = ((BitmapImpl)brush.Source.PlatformImpl).GetDirect2DBitmap(target);
            var imageSize = new Size(brush.Source.PixelWidth, brush.Source.PixelHeight);
            var tileMode = brush.TileMode;
            var sourceRect = brush.SourceRect.ToPixels(imageSize);
            var destinationRect = brush.DestinationRect.ToPixels(targetSize);
            var scale = brush.Stretch.CalculateScaling(destinationRect.Size, sourceRect.Size);
            var translate = CalculateTranslate(brush, sourceRect, destinationRect, scale);
            var intermediateSize = CalculateIntermediateSize(tileMode, targetSize, destinationRect.Size);
            var brtOpts = CompatibleRenderTargetOptions.None;

            // TODO: There are times where we don't need to draw an intermediate bitmap. Identify
            // them and directly use 'image' in those cases.
            using (var intermediate = new BitmapRenderTarget(target, brtOpts, intermediateSize))
            {
                SharpDX.RectangleF drawRect;

                intermediate.BeginDraw();
                intermediate.Transform = CalculateIntermediateTransform(
                    tileMode, 
                    sourceRect, 
                    destinationRect, 
                    scale, 
                    translate, 
                    out drawRect);
                intermediate.PushAxisAlignedClip(drawRect, AntialiasMode.Aliased);
                intermediate.DrawBitmap(image, 1, BitmapInterpolationMode.Linear);
                intermediate.PopAxisAlignedClip();
                intermediate.EndDraw();

                this.PlatformBrush = new BitmapBrush(
                    target, 
                    intermediate.Bitmap,
                    GetBitmapBrushProperties(brush),
                    GetBrushProperties(brush, destinationRect));
            }
        }

        private BitmapBrush CreateDirectBrush(
            ImageBrush brush,
            RenderTarget target,
            Bitmap image, 
            Rect sourceRect, 
            Rect destinationRect)
        {
            var tileMode = brush.TileMode;
            var scale = brush.Stretch.CalculateScaling(destinationRect.Size, sourceRect.Size);
            var translate = CalculateTranslate(brush, sourceRect, destinationRect, scale);
            var transform = Matrix.CreateTranslation(-sourceRect.Position) *
                Matrix.CreateScale(scale) *
                Matrix.CreateTranslation(translate);

            var opts = new BrushProperties
            {
                Transform = transform.ToDirect2D(),
                Opacity = (float)brush.Opacity,
            };

            var bitmapOpts = new BitmapBrushProperties
            {
                ExtendModeX = GetExtendModeX(tileMode),
                ExtendModeY = GetExtendModeY(tileMode),                
            };

            return new BitmapBrush(target, image, bitmapOpts, opts);
        }

        private BitmapBrush CreateIndirectBrush()
        {
            throw new NotImplementedException();
        }
    }
}
