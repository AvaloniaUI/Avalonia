using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataRectangleNode : RenderDataBrushAndPenNode
{
    public RoundedRect Rect { get; set; }
    public BoxShadows BoxShadows { get; set; }
    
    public override bool HitTest(Point p)
    {
        if (ServerBrush != null) // it's safe to check for null
        {
            var rect = Rect.Rect.Inflate((ClientPen?.Thickness / 2) ?? 0);
            return rect.ContainsExclusive(p);
        }
        else
        {
            var borderRect = Rect.Rect.Inflate((ClientPen?.Thickness / 2) ?? 0);
            var emptyRect = Rect.Rect.Deflate((ClientPen?.Thickness / 2) ?? 0);
            return borderRect.ContainsExclusive(p) && !emptyRect.ContainsExclusive(p);
        }
    }

    public override void Invoke(ref RenderDataNodeRenderContext context) =>
        context.Context.DrawRectangle(ServerBrush, ServerPen, Rect, BoxShadows);

    public override Rect? Bounds => BoxShadows.TransformBounds(Rect.Rect).Inflate((ServerPen?.Thickness ?? 0) / 2);
}