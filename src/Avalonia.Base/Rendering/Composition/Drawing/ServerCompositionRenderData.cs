using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing.Nodes;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

class ServerCompositionRenderData : SimpleServerRenderResource
{
    private PooledInlineList<IRenderDataItem> _items;
    private PooledInlineList<IServerRenderResource> _referencedResources;
    private Rect? _bounds;
    private bool _boundsValid;
    private static readonly ThreadSafeObjectPool<Collector> s_resourceHashSetPool = new();

    public ServerCompositionRenderData(ServerCompositor compositor) : base(compositor)
    {
    }

    class Collector : IRenderDataServerResourcesCollector
    {
        public readonly HashSet<IServerRenderResource> Resources = new();
        public void AddRenderDataServerResource(object? obj)
        {
            if (obj is IServerRenderResource res)
                Resources.Add(res);
        }
    }
    
    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        Reset();
        
        var count = reader.Read<int>();
        _items.EnsureCapacity(count);
        for (var c = 0; c < count; c++)
            _items.Add(reader.ReadObject<IRenderDataItem>());
        
        var collector = s_resourceHashSetPool.Get();
        CollectResources(_items, collector);

        foreach (var r in collector.Resources)
        {
            _referencedResources.Add(r);
            r.AddObserver(this);
        }

        collector.Resources.Clear();
        s_resourceHashSetPool.ReturnAndSetNull(ref collector);
        
        base.DeserializeChangesCore(reader, committedAt);
    }

    private static void CollectResources(PooledInlineList<IRenderDataItem> items, IRenderDataServerResourcesCollector collector)
    {
        foreach (var item in items)
        {
            if (item is IRenderDataItemWithServerResources resourceItem)
                resourceItem.Collect(collector);
            else if (item is RenderDataPushNode pushNode)
                CollectResources(pushNode.Children, collector);
        }
    }

    public Rect? Bounds
    {
        get
        {
            if (!_boundsValid)
            {
                _bounds = CalculateRenderBounds();
                _boundsValid = true;
            }
            return _bounds;
        }
    }

    private Rect? CalculateRenderBounds()
    {
        Rect? totalBounds = null;
        foreach (var item in _items) 
            totalBounds = Rect.Union(totalBounds, item.Bounds);
        
        return ApplyRenderBoundsRounding(totalBounds);
    }

    public static Rect? ApplyRenderBoundsRounding(Rect? rect)
    {
        if (rect != null)
        {
            var r = rect.Value;
            // I don't believe that it's correct to do here (rather than in CompositionVisual),
            // but it's the old behavior, so I'm keeping it for now
            return new Rect(
                new Point(Math.Floor(r.X), Math.Floor(r.Y)),
                new Point(Math.Ceiling(r.Right), Math.Ceiling(r.Bottom)));
        }

        return null;
    }

    public override void DependencyQueuedInvalidate(IServerRenderResource sender)
    {
        _boundsValid = false;
        base.DependencyQueuedInvalidate(sender);
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

    void Reset()
    {
        _bounds = null;
        _boundsValid = false;
        foreach (var r in _referencedResources)
            r.RemoveObserver(this);
        _referencedResources.Dispose();
        foreach(var i in _items)
            if (i is IDisposable disp)
                disp.Dispose();
        _items.Dispose();
    }
    
    public override void Dispose()
    {
        Reset();
        base.Dispose();
    }
}
