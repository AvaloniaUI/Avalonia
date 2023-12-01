using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataRectangleNode : RenderDataBrushAndPenNode
{
    public RoundedRect Rect { get; set; }
    public BoxShadows BoxShadows { get; set; }

    public override bool HitTest(Point p)
    {
        var strokeThicknessAdjustment = (ClientPen?.Thickness / 2) ?? 0;

        if (Rect.IsRounded)
        {
            var outerRoundedRect = Rect.Inflate(strokeThicknessAdjustment, strokeThicknessAdjustment);
            if (outerRoundedRect.ContainsExclusive(p))
            {
                if (ServerBrush != null) // it's safe to check for null
                    return true;

                var innerRoundedRect = Rect.Deflate(strokeThicknessAdjustment, strokeThicknessAdjustment);
                return !innerRoundedRect.ContainsExclusive(p);
            } 
        }
        else
        {
            var outerRect = Rect.Rect.Inflate(strokeThicknessAdjustment);
            if (outerRect.ContainsExclusive(p))
            {
                if (ServerBrush != null) // it's safe to check for null
                    return true;

                var innerRect = Rect.Rect.Deflate(strokeThicknessAdjustment);
                return !innerRect.ContainsExclusive(p);
            }
        }

        return false;
    }

    public override void Invoke(ref RenderDataNodeRenderContext context) =>
        context.Context.DrawRectangle(ServerBrush, ServerPen, Rect, BoxShadows);

    public override Rect? Bounds => BoxShadows.TransformBounds(Rect.Rect).Inflate((ServerPen?.Thickness ?? 0) / 2);
}
