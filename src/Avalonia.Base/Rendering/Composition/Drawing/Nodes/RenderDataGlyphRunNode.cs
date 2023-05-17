using System;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataGlyphRunNode : IRenderDataItemWithServerResources, IDisposable
{
    public IBrush? ServerBrush { get; set; }
    // Dispose only happens once, so it's safe to have one reference
    public IRef<IGlyphRunImpl>? GlyphRun { get; set; }

    public bool HitTest(Point p) => GlyphRun?.Item.Bounds.ContainsExclusive(p) ?? false;
    
    public void Invoke(ref RenderDataNodeRenderContext context)
    {
        Debug.Assert(GlyphRun!.Item != null);
        context.Context.DrawGlyphRun(ServerBrush, GlyphRun.Item);
    }

    public Rect? Bounds => GlyphRun?.Item?.Bounds ?? default;

    public void Collect(IRenderDataServerResourcesCollector collector)
    {
        collector.AddRenderDataServerResource(ServerBrush);
    }

    public void Dispose()
    {
        GlyphRun?.Dispose();
        GlyphRun = null;
    }
}