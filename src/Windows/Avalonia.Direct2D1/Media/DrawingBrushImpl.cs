// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;
using Avalonia.Rendering.Utilities;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public sealed class DrawingBrushImpl : BrushImpl
    {
        public DrawingBrushImpl(
            ITileBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            IDrawing drawing,
            Size targetSize)
        {
            var bounds = drawing.GetBounds();
            var calc = new TileBrushCalculator(brush, bounds.Size, targetSize);

            using (var intermediate = RenderIntermediate(target, drawing, calc))
            {
                PlatformBrush = new BitmapBrush(
                    target,
                    intermediate.Bitmap,
                    ImageBrushImpl.GetBitmapBrushProperties(brush),
                    ImageBrushImpl.GetBrushProperties(brush, calc.DestinationRect));
            }
        }

        public override void Dispose()
        {
            ((BitmapBrush)PlatformBrush)?.Bitmap.Dispose();
            base.Dispose();
        }

        private BitmapRenderTarget RenderIntermediate(
            SharpDX.Direct2D1.RenderTarget target,
            IDrawing drawing,
            TileBrushCalculator calc)
        {
            var result = new BitmapRenderTarget(
                target,
                CompatibleRenderTargetOptions.None,
                calc.IntermediateSize.ToSharpDX());

            var context = new RenderTarget(result).CreateDrawingContext(null);
            context.Clear(Colors.Transparent);

            using (var drawingContext = new DrawingContext(context))
            {
                drawingContext.PushClip(calc.IntermediateClip);
                drawingContext.PushPreTransform(calc.IntermediateTransform);
                drawing.Draw(drawingContext);
            }

            return result;
        }
    }
}