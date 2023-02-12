using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Utilities;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Avalonia.Rendering.Composition;

internal class CompositionTreeSnapshot : IAsyncDisposable
{
    public Compositor Compositor { get; }
    
    public CompositionTreeSnapshotItem Root { get; }

    public Bitmap Bitmap { get; }

    private CompositionTreeSnapshot(Compositor compositor, ServerCompositionVisual root, Bitmap bitmap)
    {
        Compositor = compositor;
        Root = new CompositionTreeSnapshotItem(this, root);
        Bitmap = bitmap;
    }
    
    public bool IsDisposed { get; private set; }
    
    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        Bitmap.Dispose();
        return new ValueTask(Compositor.InvokeServerJobAsync(() =>
        {
            Root.Destroy();
        }));
    }

    public static Task<CompositionTreeSnapshot?> TakeAsync(CompositionVisual visual)
    {
        return visual.Compositor.InvokeServerJobAsync(() =>
        {
            if (visual.Root == null)
                return null;
            Bitmap bitmap;
            var ri = visual.Compositor.Server.RenderInterface;
            using (ri.EnsureCurrent())
            {
                using (var layer =
                       ri.Value.CreateLayer(new Size(Math.Ceiling(visual.Size.X), Math.Ceiling(visual.Size.Y)), 1))
                {
                    var visualBrushHelper = new CompositorDrawingContextProxy.VisualBrushRenderer();
                    using (var context = layer.CreateDrawingContext(visualBrushHelper))
                    {
                        context.Clear(Colors.Transparent);
                        visual.Server.Render(new CompositorDrawingContextProxy(context, visualBrushHelper), new Rect(0,
                            0,
                            visual.Server.Size.X,
                            visual.Server.Size.Y));
                    }

                    using (var ms = new MemoryStream())
                    {
                        layer.Save(ms);
                        ms.Position = 0;
                        bitmap = new Bitmap(ms);
                    }
                }
            }

            return new CompositionTreeSnapshot(visual.Compositor, visual.Server, bitmap);
        });
    }

    private CompositionTreeSnapshotItem? HitTest(CompositionTreeSnapshotItem item, Point pt)
    {
        if (!MatrixUtils.ToMatrix(item.Transform).TryInvert(out var inverted))
            return null;
        pt = inverted.Transform(pt);
        if (double.IsNaN(pt.X) || double.IsNaN(pt.Y) || double.IsInfinity(pt.X) || double.IsInfinity(pt.Y))
            return null;

        if (item.ClipToBounds && (item.Size.X < pt.X || item.Size.Y < pt.Y || pt.X < 0 || pt.Y < 0))
            return null;
        
        if (item.GeometryClip?.FillContains(pt) == false && item.GeometryClip.FillContains(pt) == false)
            return null;

        for (var c = item.Children.Count - 1; c >= 0; c--)
        {
            var ch = item.Children[c];
            var chResult = HitTest(ch, pt);
            if (chResult != null)
                return chResult;
        }

        if (item.HitTest(pt))
            return item;
        return null;
    }

    public CompositionTreeSnapshotItem? HitTest(Point pt) => HitTest(Root, pt);
}

internal class CompositionTreeSnapshotItem
{
    private readonly CompositionTreeSnapshot _snapshot;
    public string? Name { get; }
    private CompositionDrawList? _drawList;
    
    internal CompositionTreeSnapshotItem(CompositionTreeSnapshot snapshot, ServerCompositionVisual visual)
    {
        _snapshot = snapshot;
        Name = (visual as ICompositionVisualWithDiagnosticsInfo)?.Name ?? visual.GetType().Name;
        _drawList = (visual as ICompositionVisualWithDrawList)?.DrawList?.Clone();
        DrawOperations = _drawList?.Select(x => x.Item.GetType().Name).ToList() ??
                         (IReadOnlyList<string>)Array.Empty<string>();
        visual.PopulateDiagnosticProperties(Properties);
        Transform = visual.CombinedTransformMatrix;
        ClipToBounds = visual.ClipToBounds;
        GeometryClip = visual.Clip;
        Size = visual.Size;
        if (visual is ServerCompositionContainerVisual container)
            Children = container.Children.List.Select(x => new CompositionTreeSnapshotItem(snapshot, x)).ToList();
        else
            Children = Array.Empty<CompositionTreeSnapshotItem>();
    }

    public bool HitTest(Point v) => _drawList?.HitTest(v) == true;
    
    public IGeometryImpl? GeometryClip { get; set; }

    public bool ClipToBounds { get; }

    public Matrix4x4 Transform { get; }
    
    public Vector2 Size { get; }

    public IReadOnlyList<string> DrawOperations { get; }
    
    public IReadOnlyList<CompositionTreeSnapshotItem> Children { get; }

    public Dictionary<string, object?> Properties { get; } = new();

    public Task<Bitmap?> RenderToBitmapAsync(int? drawOperationIndex)
    {
        if (_snapshot.IsDisposed || _drawList == null || _drawList.Count == 0)
            return Task.FromResult<Bitmap?>(null);
        return _snapshot.Compositor.InvokeServerJobAsync(() =>
        {
            using (_snapshot.Compositor.Server.RenderInterface.EnsureCurrent())
            {
                if (_snapshot.IsDisposed)
                    return null;

                var margin = 20;
                var bounds =
                    drawOperationIndex == null
                        ? _drawList.CalculateBounds()
                        : _drawList[drawOperationIndex.Value].Item.Bounds;

                using (var layer = _snapshot.Compositor.Server.RenderInterface.Value.CreateLayer(bounds.Size.Inflate(new Thickness(margin)), 1))
                {
                    var visualBrushHelper = new CompositorDrawingContextProxy.VisualBrushRenderer();
                    using (var ctx = layer.CreateDrawingContext(visualBrushHelper))
                    {
                        var proxy = new CompositorDrawingContextProxy(ctx, visualBrushHelper);
                        proxy.PostTransform = Matrix.CreateTranslation(-bounds.Left + margin, -bounds.Top + margin);
                        proxy.Transform = Matrix.Identity;
                        if (drawOperationIndex != null)
                            _drawList[drawOperationIndex.Value].Item.Render(proxy);
                        else
                            foreach (var item in _drawList)
                                item.Item.Render(proxy);
                    }

                    using (var ms = new MemoryStream())
                    {
                        layer.Save(ms);
                        ms.Position = 0;
                        return new Bitmap(
                            RefCountable.Create(AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>()
                                .LoadBitmap(ms)));
                    }
                }
            }
        });
    }

    internal void Destroy()
    {
        _drawList?.Dispose();
        _drawList = null;
        foreach (var ch in Children)
            ch.Destroy();
    }
}


