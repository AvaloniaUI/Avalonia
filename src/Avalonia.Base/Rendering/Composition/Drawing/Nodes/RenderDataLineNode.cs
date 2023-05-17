using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataLineNode : IRenderDataItemWithServerResources
{
    public IPen? ServerPen { get; set; }
    public IPen? ClientPen { get; set; }
    public Point P1 { get; set; }
    public Point P2 { get; set; }
    
    public bool HitTest(Point p)
    {
        if (ClientPen == null)
            return false;
        var halfThickness = ClientPen.Thickness / 2;
        var minX = Math.Min(P1.X, P2.X) - halfThickness;
        var maxX = Math.Max(P1.X, P2.X) + halfThickness;
        var minY = Math.Min(P1.Y, P2.Y) - halfThickness;
        var maxY = Math.Max(P1.Y, P2.Y) + halfThickness;

        if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
            return false;

        var a = P1;
        var b = P2;

        //If dot1 or dot2 is negative, then the angle between the perpendicular and the segment is obtuse.
        //The distance from a point to a straight line is defined as the
        //length of the vector formed by the point and the closest point of the segment

        Vector ap = p - a;
        var dot1 = Vector.Dot(b - a, ap);

        if (dot1 < 0)
            return ap.Length <= ClientPen.Thickness / 2;

        Vector bp = p - b;
        var dot2 = Vector.Dot(a - b, bp);

        if (dot2 < 0)
            return bp.Length <= halfThickness;

        var bXaX = b.X - a.X;
        var bYaY = b.Y - a.Y;

        var distance = (bXaX * (p.Y - a.Y) - bYaY * (p.X - a.X)) /
                       (Math.Sqrt(bXaX * bXaX + bYaY * bYaY));

        return Math.Abs(distance) <= halfThickness;
    }
    

    public void Invoke(ref RenderDataNodeRenderContext context) 
        => context.Context.DrawLine(ServerPen, P1, P2);

    public Rect? Bounds => LineBoundsHelper.CalculateBounds(P1, P2, ServerPen!);
    public void Collect(IRenderDataServerResourcesCollector collector)
    {
        collector.AddRenderDataServerResource(ServerPen);
    }
}