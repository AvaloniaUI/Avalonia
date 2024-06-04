using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionExperimentalAcrylicVisual
{
    protected override void RenderCore(CompositorDrawingContextProxy canvas, LtrbRect currentTransformedClip,
        IDirtyRectTracker dirtyRects)
    {
        var cornerRadius = CornerRadius;
        canvas.DrawRectangle(
            Material,
            new RoundedRect(
                new Rect(0, 0, Size.X, Size.Y),
                cornerRadius.TopLeft, cornerRadius.TopRight,
                cornerRadius.BottomRight, cornerRadius.BottomLeft));

        base.RenderCore(canvas, currentTransformedClip, dirtyRects);
    }

    public override LtrbRect OwnContentBounds => new(0, 0, Size.X, Size.Y);

    public ServerCompositionExperimentalAcrylicVisual(ServerCompositor compositor, Visual v) : base(compositor, v)
    {
    }
}