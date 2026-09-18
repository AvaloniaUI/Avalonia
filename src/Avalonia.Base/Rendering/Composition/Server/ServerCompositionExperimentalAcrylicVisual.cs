using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionExperimentalAcrylicVisual
{
    protected override void PushClipToBounds(IDrawingContextImpl canvas)
    {
        var clipRect = new Rect(new Size(Size.X, Size.Y));
        if (_cornerRadius == default)
        {
            canvas.PushClip(clipRect);
        }
        else
        {
            canvas.PushClip(new RoundedRect(clipRect, _cornerRadius));
        }
    }

    protected override void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
    {
        var cornerRadius = CornerRadius;
        if (context.Canvas is IDrawingContextWithAcrylicLikeSupport supported)
            supported.DrawRectangle(
                Material,
                new RoundedRect(
                    new Rect(0, 0, Size.X, Size.Y),
                    cornerRadius.TopLeft, cornerRadius.TopRight,
                    cornerRadius.BottomRight, cornerRadius.BottomLeft));

        base.RenderCore(context, currentTransformedClip);
    }

    public override LtrbRect? ComputeOwnContentBounds() =>
        LtrbRect.FullUnion(base.ComputeOwnContentBounds(), new LtrbRect(0, 0, Size.X, Size.Y));

    protected override void SizeChanged()
    {
        EnqueueForOwnBoundsRecompute();
        base.SizeChanged();
    }

    public ServerCompositionExperimentalAcrylicVisual(ServerCompositor compositor, Visual v) : base(compositor, v)
    {
    }
}
