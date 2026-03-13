using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataOpacityMaskNode : RenderDataPushNode, IRenderDataItemWithServerResources, IPoolableRenderDataItem
{
    private static readonly RenderDataNodePool<RenderDataOpacityMaskNode> s_pool = new();

    public static RenderDataOpacityMaskNode Get() => s_pool.Get();

    public IBrush? ServerBrush { get; set; }

    public Rect BoundsRect { get; set; }

    public void Collect(IRenderDataServerResourcesCollector collector)
    {
        collector.AddRenderDataServerResource(ServerBrush);
    }

    public override void Push(ref RenderDataNodeRenderContext context)
    {
        if (ServerBrush != null)
            context.Context.PushOpacityMask(ServerBrush, BoundsRect);
    }

    public override void Pop(ref RenderDataNodeRenderContext context) =>
        context.Context.PopOpacityMask();

    public void ReturnToPool()
    {
        ServerBrush = null;
        BoundsRect = default;
        s_pool.Return(this);
    }
}