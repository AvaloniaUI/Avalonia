using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing.Nodes;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal class RenderItemList
{
    private PooledInlineList<IRenderDataItem> _items;
    private Rect? _bounds;
    private bool _boundsValid;

    public void Add(IRenderDataItem item) => _items.Add(item);

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
        LtrbRect? totalBounds = null;
        foreach (var item in _items)
            totalBounds = LtrbRect.FullUnion(totalBounds, item.Bounds);

        return ServerCompositionRenderData.ApplyRenderBoundsRounding(totalBounds)?.ToRect();
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
        foreach (var item in _items)
        {
            if (item.HitTest(pt))
                return true;
        }

        return false;
    }

}
