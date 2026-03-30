using System;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

internal class RenderDataRecordingCompositionNode : IRenderDataItemWithServerResources, IDisposable
{
    public required CompositionRenderData RenderData { get; init; }

    public void Invoke(ref RenderDataNodeRenderContext context)
    {
        RenderData.Server.Render(context.Context);
    }

    public Rect? Bounds => RenderData.Server.Bounds?.ToRect();

    public bool HitTest(Point p)
    {
        var bounds = Bounds;
        return bounds.HasValue && bounds.Value.Contains(p);
    }

    public void Collect(IRenderDataServerResourcesCollector collector)
    {
        collector.AddRenderDataServerResource(RenderData.Server);
    }

    public void Dispose()
    {
        RenderData.Dispose();
    }
}
