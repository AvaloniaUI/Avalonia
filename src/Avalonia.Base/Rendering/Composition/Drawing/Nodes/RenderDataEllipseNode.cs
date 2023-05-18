using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataEllipseNode :RenderDataBrushAndPenNode
{
    public Rect Rect { get; set; }
    
    bool Contains(double dx, double dy, double radiusX, double radiusY)
    {
        var rx2 = radiusX * radiusX;
        var ry2 = radiusY * radiusY;

        var distance = ry2 * dx * dx + rx2 * dy * dy;

        return distance <= rx2 * ry2;
    }
    
    public override bool HitTest(Point p)
    {
        var center = Rect.Center;

        var strokeThickness = ClientPen?.Thickness ?? 0;

        var rx = Rect.Width / 2 + strokeThickness / 2;
        var ry = Rect.Height / 2 + strokeThickness / 2;

        var dx = p.X - center.X;
        var dy = p.Y - center.Y;

        if (Math.Abs(dx) > rx || Math.Abs(dy) > ry)
        {
            return false;
        }

        if (ServerBrush != null)
        {
            return Contains(dx, dy, rx, ry);
        }
        else if (strokeThickness > 0)
        {
            bool inStroke = Contains(dx, dy, rx, ry);

            rx = Rect.Width / 2 - strokeThickness / 2;
            ry = Rect.Height / 2 - strokeThickness / 2;

            bool inInner = Contains(dx, dy, rx, ry);

            return inStroke && !inInner;
        }
        
        return false;
    }

    public override void Invoke(ref RenderDataNodeRenderContext context) =>
        context.Context.DrawEllipse(ServerBrush, ServerPen, Rect);

    public override Rect? Bounds => Rect.Inflate(ServerPen?.Thickness ?? 0);
}