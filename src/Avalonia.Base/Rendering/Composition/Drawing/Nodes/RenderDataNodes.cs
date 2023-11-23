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

class RenderDataCustomNode : IRenderDataItem
{
    public ICustomDrawOperation? Operation { get; set; }
    public bool HitTest(Point p) => Operation?.HitTest(p) ?? false;
    public void Invoke(ref RenderDataNodeRenderContext context) => Operation?.Render(new(context.Context, false));

    public Rect? Bounds => Operation?.Bounds;
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
                if (ch is RenderDataPushNode node)
                    node.Dispose();
            Children.Dispose();
        }
    }
}

class RenderDataClipNode : RenderDataPushNode
{
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
}

class RenderDataGeometryClipNode : RenderDataPushNode
{
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
}

class RenderDataOpacityNode : RenderDataPushNode
{
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

class RenderDataRenderOptionsNode : RenderDataPushNode
{
    public RenderOptions RenderOptions { get; set; }

    public override void Push(ref RenderDataNodeRenderContext context)
    {
        context.Context.PushRenderOptions(RenderOptions);
    }

    public override void Pop(ref RenderDataNodeRenderContext context)
    {
        context.Context.PopRenderOptions();
    }
}
