using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing.Nodes;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition.Drawing;

internal class ImmediateRenderDataSceneBrushContent : ISceneBrushContent
{
    private List<IRenderDataItem>? _items;
    private readonly ThreadSafeObjectPool<List<IRenderDataItem>> _pool;

    public ImmediateRenderDataSceneBrushContent(ITileBrush brush, List<IRenderDataItem> items, Rect? rect,
        bool useScalableRasterization, ThreadSafeObjectPool<List<IRenderDataItem>> pool)
    {
        Brush = brush;
        _items = items;
        _pool = pool;
        UseScalableRasterization = useScalableRasterization;
        if (rect == null)
        {
            foreach (var i in _items)
                rect = Rect.Union(rect, i.Bounds);
            rect = ServerCompositionRenderData.ApplyRenderBoundsRounding(rect);
        }

        Rect = rect ?? default;
    }

    public ITileBrush Brush { get; }
    public Rect Rect { get; }

    public double Opacity => Brush.Opacity;
    public ITransform? Transform => Brush.Transform;
    public RelativePoint TransformOrigin => Brush.TransformOrigin;

    public void Dispose()
    {
        if(_items == null)
            return;
        foreach (var i in _items)
            (i as IDisposable)?.Dispose();
        _items.Clear();
        _pool.ReturnAndSetNull(ref _items);
    }

    void Render(IDrawingContextImpl context)
    {
        if (_items == null)
            return;
        
        var ctx = new RenderDataNodeRenderContext(context);
        try
        {
            foreach (var i in _items)
                i.Invoke(ref ctx);
        }
        finally
        {
            ctx.Dispose();
        }
    }
    
    public void Render(IDrawingContextImpl context, Matrix? transform)
    {
        if (transform.HasValue)
        {
            var oldTransform = context.Transform;
            context.Transform = transform.Value * oldTransform;
            Render(context);
            context.Transform = oldTransform;
        }
        else
            Render(context);
    }

    public bool UseScalableRasterization { get; }

}