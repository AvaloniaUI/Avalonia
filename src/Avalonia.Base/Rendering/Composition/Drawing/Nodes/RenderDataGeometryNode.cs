using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataGeometryNode : RenderDataBrushAndPenNode, IPoolableRenderDataItem
{
    private static readonly RenderDataNodePool<RenderDataGeometryNode> s_pool = new();

    public static RenderDataGeometryNode Get() => s_pool.Get();

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

    public void ReturnToPool()
    {
        ServerBrush = null;
        ServerPen = null;
        ClientPen = null;
        Geometry = null;
        s_pool.Return(this);
    }
}
