using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing.Nodes;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal class CompositionRenderData : ICompositorSerializable, IDisposable
{
    private readonly Compositor _compositor;
    private int _refCount = 1;

    public CompositionRenderData(Compositor compositor)
    {
        _compositor = compositor;
        Server = new ServerCompositionRenderData(compositor.Server);
    }

    public ServerCompositionRenderData Server { get; }
    private PooledInlineList<ICompositionRenderResource> _resources;
    private PooledInlineList<IRenderDataItem> _items;
    private bool _itemsSent;
    public void AddResource(ICompositionRenderResource resource) => _resources.Add(resource);

    public void Add(IRenderDataItem item) => _items.Add(item);

    public void AddRef() => _refCount++;

    public void Dispose()
    {
        if (--_refCount > 0)
            return;

        if (!_itemsSent)
        {
            foreach(var i in _items)
                if (i is IDisposable disp)
                    disp.Dispose();
        }

        _items.Dispose();
        _itemsSent = false;
        foreach(var r in _resources)
            r.ReleaseOnCompositor(_compositor);
        _resources.Dispose();

        _compositor.DisposeOnNextBatch(Server);
    }

    public SimpleServerObject TryGetServer(Compositor c) => Server;

    public void SerializeChanges(Compositor c, BatchStreamWriter writer)
    {
        writer.Write(_items.Count);
        foreach (var item in _items) 
            writer.WriteObject(item);
        _itemsSent = true;
    }

    public Rect? Bounds
    {
        get
        {
            LtrbRect? totalBounds = null;
            foreach (var item in _items)
                totalBounds = LtrbRect.FullUnion(totalBounds, item.Bounds);
            return ServerCompositionRenderData.ApplyRenderBoundsRounding(totalBounds)?.ToRect();
        }
    }

    public void Render(IDrawingContextImpl context)
    {
        var ctx = new RenderDataNodeRenderContext(context);
        try
        {
            foreach (var item in _items)
                item.Invoke(ref ctx);
        }
        finally
        {
            ctx.Dispose();
        }
    }

    public bool HitTest(Point pt)
    {
        foreach (var op in _items)
        {
            if (op.HitTest(pt))
                return true;
        }

        return false;
    }
}