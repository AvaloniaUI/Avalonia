using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataOpacityMaskNode : RenderDataPushNode, IRenderDataItemWithServerResources
{
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
}