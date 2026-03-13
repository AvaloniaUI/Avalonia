using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

enum RenderDataPopNodeType
{
    Transform,
    Clip,
    GeometryClip,
    Opacity,
    OpacityMask
}

interface IRenderDataServerResourcesCollector
{
    void AddRenderDataServerResource(object? obj);
}

interface IRenderDataItemWithServerResources : IRenderDataItem
{
    void Collect(IRenderDataServerResourcesCollector collector);
}

struct RenderDataNodeRenderContext : IDisposable
{
    private Stack<Matrix>? _stack;
    private static readonly ThreadSafeObjectPool<Stack<Matrix>> s_matrixStackPool = new();
    
    public RenderDataNodeRenderContext(IDrawingContextImpl context)
    {
        Context = context;
    }
    public IDrawingContextImpl Context { get; }

    public Stack<Matrix> MatrixStack
    {
        get => _stack ??= s_matrixStackPool.Get();
    }

    public void Dispose()
    {
        if (_stack != null)
        {
            _stack.Clear();
            s_matrixStackPool.ReturnAndSetNull(ref _stack);
        }
    }
}

/// <summary>
/// Implemented by render data nodes that support object pooling to reduce GC pressure.
/// </summary>
interface IPoolableRenderDataItem
{
    void ReturnToPool();
}

interface IRenderDataItem
{
    /// <summary>
    /// Renders the node to a drawing context.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    void Invoke(ref RenderDataNodeRenderContext context);
    
    /// <summary>
    /// Gets the bounds of the visible content in the node in global coordinates.
    /// </summary>
    Rect? Bounds { get; }
    
    /// <summary>
    /// Hit test the geometry in this node.
    /// </summary>
    /// <param name="p">The point in global coordinates.</param>
    /// <returns>True if the point hits the node's geometry; otherwise false.</returns>
    /// <remarks>
    /// This method does not recurse to childs, if you want
    /// to hit test children they must be hit tested manually.
    /// </remarks>
    bool HitTest(Point p);
}

class RenderDataCustomNode : IRenderDataItem, IDisposable, IPoolableRenderDataItem
{
    private static readonly RenderDataNodePool<RenderDataCustomNode> s_pool = new();

    public static RenderDataCustomNode Get() => s_pool.Get();

    public ICustomDrawOperation? Operation { get; set; }
    public bool HitTest(Point p) => Operation?.HitTest(p) ?? false;
    public void Invoke(ref RenderDataNodeRenderContext context) => Operation?.Render(new(context.Context, false));

    public Rect? Bounds => Operation?.Bounds;

    public void Dispose()
    {
        Operation?.Dispose();
        Operation = null;
    }

    public void ReturnToPool()
    {
        Dispose();
        s_pool.Return(this);
    }
}

abstract class RenderDataPushNode : IRenderDataItem, IDisposable
{
    public PooledInlineList<IRenderDataItem> Children;
    public abstract void Push(ref RenderDataNodeRenderContext context);
    public abstract void Pop(ref RenderDataNodeRenderContext context);
    public void Invoke(ref RenderDataNodeRenderContext context)
    {
        if (Children.Count == 0)
            return;
        Push(ref context);
        foreach (var ch in Children) 
            ch.Invoke(ref context);
        Pop(ref context);
    }

    public virtual Rect? Bounds
    {
        get
        {
            if (Children.Count == 0)
                return null;
            Rect? union = null;
            foreach (var i in Children)
                union = Rect.Union(union, i.Bounds);
            return union;
        }
    }

    public virtual bool HitTest(Point p)
    {
        if (Children.Count == 0)
            return false;
        foreach(var ch in Children)
            if (ch.HitTest(p))
                return true;
        return false;
    }

    public void Dispose()
    {
        if (Children.Count > 0)
        {
            foreach(var ch in Children)
                if (ch is IDisposable disposable)
                    disposable.Dispose();
            Children.Dispose();
        }
    }
}

class RenderDataClipNode : RenderDataPushNode, IPoolableRenderDataItem
{
    private static readonly RenderDataNodePool<RenderDataClipNode> s_pool = new();

    public static RenderDataClipNode Get() => s_pool.Get();

    public RoundedRect Rect { get; set; }
    public override void Push(ref RenderDataNodeRenderContext context) =>
        context.Context.PushClip(Rect);

    public override void Pop(ref RenderDataNodeRenderContext context) =>
        context.Context.PopClip();

    public override bool HitTest(Point p)
    {
        if (!Rect.Rect.Contains(p))
            return false;
        return base.HitTest(p);
    }

    public void ReturnToPool()
    {
        Rect = default;
        s_pool.Return(this);
    }
}

class RenderDataGeometryClipNode : RenderDataPushNode, IPoolableRenderDataItem
{
    private static readonly RenderDataNodePool<RenderDataGeometryClipNode> s_pool = new();

    public static RenderDataGeometryClipNode Get() => s_pool.Get();

    public IGeometryImpl? Geometry { get; set; }
    public bool Contains(Point p) => Geometry?.FillContains(p) ?? false;

    public override void Push(ref RenderDataNodeRenderContext context)
    {
        if (Geometry != null)
            context.Context.PushGeometryClip(Geometry);
    }

    public override void Pop(ref RenderDataNodeRenderContext context)
    {
        if (Geometry != null)
            context.Context.PopGeometryClip();
    }

    public override bool HitTest(Point p)
    {
        if (Geometry != null && !Geometry.FillContains(p))
            return false;
        return base.HitTest(p);
    }

    public void ReturnToPool()
    {
        Geometry = null;
        s_pool.Return(this);
    }
}

class RenderDataOpacityNode : RenderDataPushNode, IPoolableRenderDataItem
{
    private static readonly RenderDataNodePool<RenderDataOpacityNode> s_pool = new();

    public static RenderDataOpacityNode Get() => s_pool.Get();

    public double Opacity { get; set; }
    public override void Push(ref RenderDataNodeRenderContext context)
    {
        if (Opacity != 1)
            context.Context.PushOpacity(Opacity, null);
    }

    public override void Pop(ref RenderDataNodeRenderContext context)
    {
        if (Opacity != 1)
            context.Context.PopOpacity();
    }

    public void ReturnToPool()
    {
        Opacity = default;
        s_pool.Return(this);
    }
}

abstract class RenderDataBrushAndPenNode : IRenderDataItemWithServerResources
{
    public IBrush? ServerBrush { get; set; }
    public IPen? ServerPen { get; set; }
    public IPen? ClientPen { get; set; }
    
    public void Collect(IRenderDataServerResourcesCollector collector)
    {
        collector.AddRenderDataServerResource(ServerBrush);
        collector.AddRenderDataServerResource(ServerPen);
    }

    public abstract void Invoke(ref RenderDataNodeRenderContext context);
    public abstract Rect? Bounds { get; }
    public abstract bool HitTest(Point p);
}

class RenderDataRenderOptionsNode : RenderDataPushNode, IPoolableRenderDataItem
{
    private static readonly RenderDataNodePool<RenderDataRenderOptionsNode> s_pool = new();

    public static RenderDataRenderOptionsNode Get() => s_pool.Get();

    public RenderOptions RenderOptions { get; set; }

    public override void Push(ref RenderDataNodeRenderContext context)
    {
        context.Context.PushRenderOptions(RenderOptions);
    }

    public override void Pop(ref RenderDataNodeRenderContext context)
    {
        context.Context.PopRenderOptions();
    }

    public void ReturnToPool()
    {
        RenderOptions = default;
        s_pool.Return(this);
    }
}

class RenderDataTextOptionsNode : RenderDataPushNode, IPoolableRenderDataItem
{
    private static readonly RenderDataNodePool<RenderDataTextOptionsNode> s_pool = new();

    public static RenderDataTextOptionsNode Get() => s_pool.Get();

    public TextOptions TextOptions { get; set; }

    public override void Push(ref RenderDataNodeRenderContext context)
    {
        context.Context.PushTextOptions(TextOptions);
    }

    public override void Pop(ref RenderDataNodeRenderContext context)
    {
        context.Context.PopTextOptions();
    }

    public void ReturnToPool()
    {
        TextOptions = default;
        s_pool.Return(this);
    }
}

static class RenderDataItemPoolHelper
{
    /// <summary>
    /// Disposes disposable items and returns poolable items to their object pools.
    /// Recurses into push node children.
    /// </summary>
    public static void DisposeAndReturnToPool(PooledInlineList<IRenderDataItem> items)
    {
        foreach (var item in items)
        {
            if (item is RenderDataPushNode pushNode)
            {
                DisposeAndReturnToPool(pushNode.Children);
                pushNode.Children.Dispose();
            }

            if (item is IPoolableRenderDataItem poolable)
                poolable.ReturnToPool();
            else if (item is IDisposable disp)
                disp.Dispose();
        }
    }
}
