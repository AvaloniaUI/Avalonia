using System;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

internal class RenderDataRecordingNode : IRenderDataItemWithServerResources, IDisposable
{
    public ServerCompositionRenderData? ServerRenderData { get; set; }

    public void Invoke(ref RenderDataNodeRenderContext context)
    {
        ServerRenderData?.Render(context.Context);
    }

    public Rect? Bounds => ServerRenderData?.Bounds?.ToRect();

    public bool HitTest(Point p)
    {
        var bounds = Bounds;
        return bounds.HasValue && bounds.Value.Contains(p);
    }

    public void Collect(IRenderDataServerResourcesCollector collector)
    {
        collector.AddRenderDataServerResource(ServerRenderData);
    }

    public void Dispose()
    {
        ServerRenderData = null;
    }
}
