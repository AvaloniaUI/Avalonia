using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing.Nodes;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal class RenderDataDrawingContext : DrawingContext
{
    private readonly Compositor? _compositor;
    private CompositionRenderData? _renderData;
    private HashSet<object>? _resourcesHashSet;
    private static readonly ThreadSafeObjectPool<HashSet<object>> s_hashSetPool = new();
    private CompositionRenderData RenderData
    {
        get
        {
            Debug.Assert(_compositor != null);
            return _renderData ??= new(_compositor);
        }
    }
    
    struct ParentStackItem
    {
        public RenderDataPushNode? Node;
        public List<IRenderDataItem> Items;
    }
    
    private List<IRenderDataItem>? _currentItemList;
    private static readonly ThreadSafeObjectPool<List<IRenderDataItem>> s_listPool = new();

    private Stack<ParentStackItem>? _parentNodeStack;
    private static readonly ThreadSafeObjectPool<Stack<ParentStackItem>> s_parentStackPool = new();

    public RenderDataDrawingContext(Compositor? compositor)
    {
        _compositor = compositor;
    }

    void Add(IRenderDataItem item)
    {
        _currentItemList ??= s_listPool.Get();
        _currentItemList.Add(item);
    }

    void Push(RenderDataPushNode? node = null)
    {
        // Push a fake no-op node so something could be popped by the corresponding Pop call
        // Since there is no nesting, we don't update the item list
        if (node == null)
        {
            (_parentNodeStack ??= s_parentStackPool.Get()).Push(default);
            return;
        }    
        Add(node);
        (_parentNodeStack ??= s_parentStackPool.Get()).Push(new ParentStackItem
        {
            Node = node,
            Items = _currentItemList!
        });
        _currentItemList = null;
    }

    void Pop<T>() where T : IRenderDataItem
    {
        var parent = _parentNodeStack!.Pop();
        
        // No-op node
        if (parent.Node == null)
            return;

        if (!(parent.Node is T))
            throw new InvalidOperationException("Invalid Pop operation");

        var removeLastPush = true;
        if (_currentItemList != null)
        {
            removeLastPush = _currentItemList.Count == 0;
            foreach (var item in _currentItemList)
                parent.Node.Children.Add(item);
            _currentItemList.Clear();
            s_listPool.ReturnAndSetNull(ref _currentItemList);
        }
        _currentItemList = parent.Items;
        if (removeLastPush)
            _currentItemList.RemoveAt(_currentItemList.Count - 1);
    }

    void AddResource(object? resource)
    {
        if (_compositor == null)
            return;
        
        if (resource == null
            || resource is IImmutableBrush
            || resource is ImmutablePen
            || resource is ImmutableTransform)
            return;
        
        if (resource is ICompositionRenderResource renderResource)
        {
            _resourcesHashSet ??= s_hashSetPool.Get();
            if (!_resourcesHashSet.Add(renderResource))
                return;
            
            renderResource.AddRefOnCompositor(_compositor);
            RenderData.AddResource(renderResource);
            return;
        }

        throw new InvalidOperationException(resource.GetType().FullName + " can not be used with this DrawingContext");
    }
    
    protected override void DrawLineCore(IPen? pen, Point p1, Point p2)
    {
        if(pen == null)
            return;
        AddResource(pen);
        Add(new RenderDataLineNode
        {
            ClientPen = pen,
            ServerPen = pen.GetServer(_compositor),
            P1 = p1,
            P2 = p2
        });
    }

    protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry)
    {
        if (brush == null && pen == null)
            return;
        AddResource(brush);
        AddResource(pen);
        Add(new RenderDataGeometryNode
        {
            ServerBrush = brush.GetServer(_compositor),
            ServerPen = pen.GetServer(_compositor),
            ClientPen = pen,
            Geometry = geometry
        });
    }

    protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default)
    {
        if (rrect.IsEmpty())
            return;
        if(brush == null && pen == null && boxShadows == default)
            return;
        AddResource(brush);
        AddResource(pen);
        Add(new RenderDataRectangleNode
        {
            ServerBrush = brush.GetServer(_compositor),
            ServerPen = pen.GetServer(_compositor),
            ClientPen = pen,
            Rect = rrect,
            BoxShadows = boxShadows
        });
    }

    protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect)
    {
        if (rect.IsEmpty())
            return;
        
        if(brush == null && pen == null)
            return;
        AddResource(brush);
        AddResource(pen);
        Add(new RenderDataEllipseNode
        {
            ServerBrush = brush.GetServer(_compositor),
            ServerPen = pen.GetServer(_compositor),
            ClientPen = pen,
            Rect = rect,
        });
    }

    public override void Custom(ICustomDrawOperation custom) => Add(new RenderDataCustomNode
    {
        Operation = custom
    });

    public override void DrawGlyphRun(IBrush? foreground, GlyphRun? glyphRun)
    {
        if (foreground == null || glyphRun == null)
            return;
        AddResource(foreground);
        Add(new RenderDataGlyphRunNode
        {
            ServerBrush = foreground.GetServer(_compositor),
            GlyphRun = glyphRun.PlatformImpl.Clone()
        });
    }

    protected override void PushClipCore(RoundedRect rect) => Push(new RenderDataClipNode
    {
        Rect = rect
    });

    protected override void PushClipCore(Rect rect) => Push(new RenderDataClipNode
    {
        Rect = rect
    });

    protected override void PushGeometryClipCore(Geometry? clip)
    {
        if (clip == null)
            Push();
        else
            Push(new RenderDataGeometryClipNode
            {
                Geometry = clip?.PlatformImpl
            });
    }

    protected override void PushOpacityCore(double opacity)
    {
        if (opacity == 1)
            Push();
        else
            Push(new RenderDataOpacityNode
            {
                Opacity = opacity
            });
    }

    protected override void PushOpacityMaskCore(IBrush? mask, Rect bounds)
    {
        if(mask == null)
            Push();
        else
        {
            AddResource(mask);
            Push(new RenderDataOpacityMaskNode
            {
                ServerBrush = mask.GetServer(_compositor),
                BoundsRect = bounds
            });
        }
    }

    protected override void PushTransformCore(Matrix matrix)
    {
        if (matrix.IsIdentity)
            Push();
        else
            Push(new RenderDataPushMatrixNode()
            {
                Matrix = matrix
            });
    }

    protected override void PushRenderOptionsCore(RenderOptions renderOptions) => Push(new RenderDataRenderOptionsNode()
    {
        RenderOptions = renderOptions
    });


    protected override void PopClipCore() => Pop<RenderDataClipNode>();

    protected override void PopGeometryClipCore() => Pop<RenderDataGeometryClipNode>();

    protected override void PopOpacityCore() => Pop<RenderDataOpacityNode>();

    protected override void PopOpacityMaskCore() => Pop<RenderDataOpacityMaskNode>();

    protected override void PopTransformCore() => Pop<RenderDataPushMatrixNode>();

    protected override void PopRenderOptionsCore() => Pop<RenderDataRenderOptionsNode>();

    internal override void DrawBitmap(IRef<IBitmapImpl>? source, double opacity, Rect sourceRect, Rect destRect)
    {
        if (source == null || sourceRect.IsEmpty() || destRect.IsEmpty())
            return;
        Add(new RenderDataBitmapNode
        {
            Bitmap = source.Clone(),
            Opacity = opacity,
            SourceRect = sourceRect,
            DestRect = destRect
        });
    }


    void FlushStack()
    {
        // Flush stack
        if (_parentNodeStack != null)
        {
            // TODO: throw error, unbalanced stack
            while (_parentNodeStack.Count > 0) 
                Pop<IRenderDataItem>();
        }
        

    }
    
    public CompositionRenderData? GetRenderResults()
    {
        Debug.Assert(_compositor != null);
        
        FlushStack();
        
        // Transfer items to RenderData
        if (_currentItemList is { Count: > 0 })
        {
            foreach (var i in _currentItemList)
                RenderData.Add(i);
            _currentItemList.Clear();
        }

        var rv = _renderData;
        _renderData = null;
        _resourcesHashSet?.Clear();
        
        if (rv != null)
            _compositor.RegisterForSerialization(rv);
        return rv;
    }

    public ImmediateRenderDataSceneBrushContent? GetImmediateSceneBrushContent(ITileBrush brush, Rect? rect, bool useScalableRasterization)
    {
        Debug.Assert(_compositor == null);
        Debug.Assert(_resourcesHashSet == null);
        Debug.Assert(_renderData == null);
        
        FlushStack();
        if (_currentItemList == null || _currentItemList.Count == 0)
            return null;

        var itemList = _currentItemList;
        _currentItemList = null;

        return new ImmediateRenderDataSceneBrushContent(brush, itemList, rect, useScalableRasterization, s_listPool);
    }

    public void Reset()
    {
        // This means that render data should be discarded
        if (_renderData != null)
        {
            _renderData.Dispose();
            _renderData = null;
        }

        _currentItemList?.Clear();
        _parentNodeStack?.Clear();
        _resourcesHashSet?.Clear();
    }
    
    protected override void DisposeCore()
    {
        Reset();
        if (_resourcesHashSet != null) 
            s_hashSetPool.ReturnAndSetNull(ref _resourcesHashSet);
    }
}
