using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

internal class RenderDataRecordingCompositionNode : IRenderDataItemWithServerResources
{
    public required ServerCompositionRenderData Server { get; init; }

    public void Invoke(ref RenderDataNodeRenderContext context)
    {
        Server.Render(context.Context);
    }

    public Rect? Bounds => Server.Bounds?.ToRect();

    public bool HitTest(Point p)
    {
        var bounds = Bounds;
        return bounds.HasValue && bounds.Value.Contains(p);
    }

    public void Collect(IRenderDataServerResourcesCollector collector)
    {
        collector.AddRenderDataServerResource(Server);
    }
}
