using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataGeometryNode : RenderDataBrushAndPenNode
{
    public IGeometryImpl? Geometry { get; set; }
    
    public override bool HitTest(Point p)
    {
        if (Geometry == null)
            return false;
        
        return (ServerBrush != null // null check is safe
                && Geometry.FillContains(p)) ||
               (ClientPen != null && Geometry.StrokeContains(ClientPen, p));
    }
    
    public override void Invoke(ref RenderDataNodeRenderContext context)
    {
        Debug.Assert(Geometry != null);
        context.Context.DrawGeometry(ServerBrush, ServerPen, Geometry!);
    }

    public override Rect? Bounds => Geometry?.GetRenderBounds(ServerPen) ?? default;
}