// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Layout;
using Perspex.Media;
using SharpDX;
using SharpDX.Direct2D1;
using Perspex.RenderHelpers;


namespace Perspex.Direct2D1.Media
{
    public sealed class TileBrushImpl : BrushImpl
    {
        public TileBrushImpl(
            TileBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            Size targetSize)
        {
            var helper = new TileBrushImplHelper(brush, targetSize);
            if (!helper.IsValid)
                return;

            using (var intermediate = new BitmapRenderTarget(target, CompatibleRenderTargetOptions.None, helper.IntermediateSize.ToSharpDX()))
            {
                using (var ctx = new RenderTarget(intermediate).CreateDrawingContext())
                    helper.DrawIntermediate(ctx);

                PlatformBrush = new BitmapBrush(
                    target,
                    intermediate.Bitmap,
                    GetBitmapBrushProperties(brush),
                    GetBrushProperties(brush, helper.DestinationRect));
            }
        }


        protected static BrushProperties GetBrushProperties(TileBrush brush, Rect destinationRect)
        {
            return new BrushProperties
            {
                Opacity = (float)brush.Opacity,
                Transform = brush.TileMode != TileMode.None ?
                    SharpDX.Matrix3x2.Translation(
                        (float)destinationRect.X,
                        (float)destinationRect.Y) :
                    SharpDX.Matrix3x2.Identity,
            };
        }

        protected static BitmapBrushProperties GetBitmapBrushProperties(TileBrush brush)
        {
            var tileMode = brush.TileMode;

            return new BitmapBrushProperties
            {
                ExtendModeX = GetExtendModeX(tileMode),
                ExtendModeY = GetExtendModeY(tileMode),
            };
        }

        protected static ExtendMode GetExtendModeX(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipX) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        protected static ExtendMode GetExtendModeY(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipY) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        public override void Dispose()
        {
            ((BitmapBrush)PlatformBrush)?.Bitmap.Dispose();
            base.Dispose();
        }
    }
}
