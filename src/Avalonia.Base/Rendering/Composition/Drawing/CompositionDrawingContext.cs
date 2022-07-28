using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Avalonia.VisualTree;

// Special license applies, see //file: src/Avalonia.Base/Rendering/Composition/License.md

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An IDrawingContextImpl implementation that builds <see cref="CompositionDrawList"/>
/// </summary>
internal class CompositionDrawingContext : IDrawingContextImpl, IDrawingContextWithAcrylicLikeSupport
{
    private CompositionDrawListBuilder _builder = new();
    private int _drawOperationIndex;

    /// <inheritdoc/>
    public Matrix Transform { get; set; } = Matrix.Identity;

    /// <inheritdoc/>
    public void Clear(Color color)
    {
        // Cannot clear a deferred scene.
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to do here since we allocate no unmanaged resources.
    }

    public void BeginUpdate(CompositionDrawList? list)
    {
        _builder.Reset(list);
        _drawOperationIndex = 0;
    }

    public CompositionDrawList EndUpdate()
    {
        _builder.TrimTo(_drawOperationIndex);
        return _builder.DrawOperations!;
    }

    /// <inheritdoc/>
    public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry)
    {
        var next = NextDrawAs<GeometryNode>();

        if (next == null || !next.Item.Equals(Transform, brush, pen, geometry))
        {
            Add(new GeometryNode(Transform, brush, pen, geometry, CreateChildScene(brush)));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect,
        BitmapInterpolationMode bitmapInterpolationMode)
    {
        var next = NextDrawAs<ImageNode>();

        if (next == null ||
            !next.Item.Equals(Transform, source, opacity, sourceRect, destRect, bitmapInterpolationMode))
        {
            Add(new ImageNode(Transform, source, opacity, sourceRect, destRect, bitmapInterpolationMode));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void DrawBitmap(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect sourceRect)
    {
        // This method is currently only used to composite layers so shouldn't be called here.
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public void DrawLine(IPen pen, Point p1, Point p2)
    {
        var next = NextDrawAs<LineNode>();

        if (next == null || !next.Item.Equals(Transform, pen, p1, p2))
        {
            Add(new LineNode(Transform, pen, p1, p2, CreateChildScene(pen.Brush)));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect,
        BoxShadows boxShadows = default)
    {
        var next = NextDrawAs<RectangleNode>();

        if (next == null || !next.Item.Equals(Transform, brush, pen, rect, boxShadows))
        {
            Add(new RectangleNode(Transform, brush, pen, rect, boxShadows, CreateChildScene(brush)));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
    {
        var next = NextDrawAs<ExperimentalAcrylicNode>();

        if (next == null || !next.Item.Equals(Transform, material, rect))
        {
            Add(new ExperimentalAcrylicNode(Transform, material, rect));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect)
    {
        var next = NextDrawAs<EllipseNode>();

        if (next == null || !next.Item.Equals(Transform, brush, pen, rect))
        {
            Add(new EllipseNode(Transform, brush, pen, rect, CreateChildScene(brush)));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    public void Custom(ICustomDrawOperation custom)
    {
        var next = NextDrawAs<CustomDrawOperation>();
        if (next == null || !next.Item.Equals(Transform, custom))
            Add(new CustomDrawOperation(custom, Transform));
        else
            ++_drawOperationIndex;
    }

    /// <inheritdoc/>
    public void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
    {
        var next = NextDrawAs<GlyphRunNode>();

        if (next == null || !next.Item.Equals(Transform, foreground, glyphRun))
        {
            Add(new GlyphRunNode(Transform, foreground, glyphRun, CreateChildScene(foreground)));
        }

        else
        {
            ++_drawOperationIndex;
        }
    }

    public IDrawingContextLayerImpl CreateLayer(Size size)
    {
        throw new NotSupportedException("Creating layers on a deferred drawing context not supported");
    }

    /// <inheritdoc/>
    public void PopClip()
    {
        var next = NextDrawAs<ClipNode>();

        if (next == null || !next.Item.Equals(null))
        {
            Add(new ClipNode());
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void PopGeometryClip()
    {
        var next = NextDrawAs<GeometryClipNode>();

        if (next == null || !next.Item.Equals(null))
        {
            Add(new GeometryClipNode());
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void PopBitmapBlendMode()
    {
        var next = NextDrawAs<BitmapBlendModeNode>();

        if (next == null || !next.Item.Equals(null))
        {
            Add(new BitmapBlendModeNode());
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void PopOpacity()
    {
        var next = NextDrawAs<OpacityNode>();

        if (next == null || !next.Item.Equals(null))
        {
            Add(new OpacityNode());
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void PopOpacityMask()
    {
        var next = NextDrawAs<OpacityMaskNode>();

        if (next == null || !next.Item.Equals(null, null))
        {
            Add(new OpacityMaskNode());
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void PushClip(Rect clip)
    {
        var next = NextDrawAs<ClipNode>();

        if (next == null || !next.Item.Equals(Transform, clip))
        {
            Add(new ClipNode(Transform, clip));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc />
    public void PushClip(RoundedRect clip)
    {
        var next = NextDrawAs<ClipNode>();

        if (next == null || !next.Item.Equals(Transform, clip))
        {
            Add(new ClipNode(Transform, clip));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void PushGeometryClip(IGeometryImpl? clip)
    {
        if (clip is null)
            return;

        var next = NextDrawAs<GeometryClipNode>();

        if (next == null || !next.Item.Equals(Transform, clip))
        {
            Add(new GeometryClipNode(Transform, clip));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void PushOpacity(double opacity)
    {
        var next = NextDrawAs<OpacityNode>();

        if (next == null || !next.Item.Equals(opacity))
        {
            Add(new OpacityNode(opacity));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void PushOpacityMask(IBrush mask, Rect bounds)
    {
        var next = NextDrawAs<OpacityMaskNode>();

        if (next == null || !next.Item.Equals(mask, bounds))
        {
            Add(new OpacityMaskNode(mask, bounds, CreateChildScene(mask)));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    public void PushBitmapBlendMode(BitmapBlendingMode blendingMode)
    {
        var next = NextDrawAs<BitmapBlendModeNode>();

        if (next == null || !next.Item.Equals(blendingMode))
        {
            Add(new BitmapBlendModeNode(blendingMode));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    private void Add<T>(T node) where T : class, IDrawOperation
    {
        if (_drawOperationIndex < _builder.Count)
        {
            _builder.ReplaceDrawOperation(_drawOperationIndex, node);
        }
        else
        {
            _builder.AddDrawOperation(node);
        }

        ++_drawOperationIndex;
    }

    private IRef<T>? NextDrawAs<T>() where T : class, IDrawOperation
    {
        return _drawOperationIndex < _builder.Count
            ? _builder.DrawOperations![_drawOperationIndex] as IRef<T>
            : null;
    }
    
    private IDisposable? CreateChildScene(IBrush? brush)
    {
        if (brush is VisualBrush visualBrush)
        {
            var visual = visualBrush.Visual;

            if (visual != null)
            {
                // TODO: This is a temporary solution to make visual brush to work like it does with DeferredRenderer
                // We should directly reference the corresponding CompositionVisual (which should
                // be attached to the same composition target) like UWP does.
                // Render-able visuals shouldn't be dangling unattached
                (visual as IVisualBrushInitialize)?.EnsureInitialized();
                
                var recorder = new CompositionDrawingContext();
                recorder.BeginUpdate(null);
                ImmediateRenderer.Render(visual, new DrawingContext(recorder));
                var drawList = recorder.EndUpdate();
                drawList.Size = visual.Bounds.Size;

                return drawList;
            }
        }
        return null;
    }
}