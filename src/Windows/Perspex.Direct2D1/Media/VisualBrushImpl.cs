// -----------------------------------------------------------------------
// <copyright file="VisualBrushImpl.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using Perspex.Layout;
    using Perspex.Media;
    using SharpDX.Direct2D1;

    public class VisualBrushImpl : BrushImpl
    {
        public VisualBrushImpl(
            Perspex.Media.VisualBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            Size destinationSize)
            : base(brush, target, destinationSize)
        {
            var visual = brush.Visual;
            var layoutable = visual as ILayoutable;

            if (layoutable?.IsArrangeValid == false)
            {
                layoutable.Measure(Size.Infinity);
                layoutable.Arrange(new Rect(layoutable.DesiredSize));
            }

            var sourceSize = layoutable.Bounds.Size;
            var destinationRect = brush.DestinationRect.ToPixels(destinationSize);
            var scale = brush.Stretch.CalculateScaling(destinationRect.Size, sourceSize);

            using (var brt = new BitmapRenderTarget(
                target,
                CompatibleRenderTargetOptions.None,
                destinationRect.Size.ToSharpDX()))
            {
                var renderer = new Renderer(brt);
                renderer.Render(visual, null, Matrix.Identity, Matrix.CreateScale(scale));
                this.PlatformBrush = new BitmapBrush(brt, brt.Bitmap);
            }
        }

        public override void Dispose()
        {
            ((BitmapBrush)this.PlatformBrush).Bitmap.Dispose();
            base.Dispose();
        }
    }
}
