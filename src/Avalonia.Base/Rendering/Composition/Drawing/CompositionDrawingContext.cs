using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Avalonia.Threading;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An IDrawingContextImpl implementation that builds <see cref="CompositionDrawList"/>
/// </summary>
internal sealed class CompositionDrawingContext : DrawingContext, IDrawingContextWithAcrylicLikeSupport
{
    private CompositionDrawListBuilder _builder = new();
    private int _drawOperationIndex;
    
    private static ThreadSafeObjectPool<Stack<Matrix>> TransformStackPool { get; } =
        ThreadSafeObjectPool<Stack<Matrix>>.Default;

    private Stack<Matrix>? _transforms;

    private static ThreadSafeObjectPool<Stack<bool>> OpacityMaskPopStackPool { get; } =
        ThreadSafeObjectPool<Stack<bool>>.Default;

    private Stack<bool>? _needsToPopOpacityMask;

    public Matrix Transform { get; set; } = Matrix.Identity;
    
    public void BeginUpdate(CompositionDrawList? list)
    {
        _builder.Reset(list);
        _drawOperationIndex = 0;
    }

    public CompositionDrawList? EndUpdate()
    {
        // Make sure that any pending pop operations are completed
        Dispose();
        
        _builder.TrimTo(_drawOperationIndex);
        return _builder.DrawOperations;
    }
    
    protected override void DisposeCore()
    {
        if (_transforms != null)
        {
            _transforms.Clear();
            TransformStackPool.ReturnAndSetNull(ref _transforms);
        }

        if (_needsToPopOpacityMask != null)
        {
            _needsToPopOpacityMask.Clear();
            _needsToPopOpacityMask = null;
        }
    }
    
    protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry)
    {
        var next = NextDrawAs<GeometryNode>();

        if (next == null || !next.Item.Equals(Transform, brush, pen, geometry))
        {
            Add(new GeometryNode(Transform, ConvertBrush(brush), pen, geometry));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    internal override void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect,
        BitmapInterpolationMode bitmapInterpolationMode = BitmapInterpolationMode.Default)
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
    protected override void DrawLineCore(IPen? pen, Point p1, Point p2)
    {
        if (pen is null)
        {
            return;
        }

        var next = NextDrawAs<LineNode>();

        if (next == null || !next.Item.Equals(Transform, pen, p1, p2))
        {
            Add(new LineNode(Transform, pen, p1, p2));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    /// <inheritdoc/>
    protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rect,
        BoxShadows boxShadows = default)
    {
        var next = NextDrawAs<RectangleNode>();

        if (next == null || !next.Item.Equals(Transform, brush, pen, rect, boxShadows))
        {
            Add(new RectangleNode(Transform, ConvertBrush(brush), pen, rect, boxShadows));
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

    protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect)
    {
        var next = NextDrawAs<EllipseNode>();

        if (next == null || !next.Item.Equals(Transform, brush, pen, rect))
        {
            Add(new EllipseNode(Transform, ConvertBrush(brush), pen, rect));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }
    
    public override void Custom(ICustomDrawOperation custom)
    {
        var next = NextDrawAs<CustomDrawOperation>();
        if (next == null || !next.Item.Equals(Transform, custom))
            Add(new CustomDrawOperation(custom, Transform));
        else
            ++_drawOperationIndex;
    }

    public override void DrawGlyphRun(IBrush? foreground, GlyphRun glyphRun)
    {
        if (foreground is null)
        {
            return;
        }

        var next = NextDrawAs<GlyphRunNode>();

        if (next == null || !next.Item.Equals(Transform, foreground, glyphRun.PlatformImpl))
        {
            Add(new GlyphRunNode(Transform, ConvertBrush(foreground)!, glyphRun.PlatformImpl));
        }

        else
        {
            ++_drawOperationIndex;
        }
    }

    protected override void PushTransformCore(Matrix matrix)
    {
        _transforms ??= TransformStackPool.Get();
        _transforms.Push(Transform);
        Transform = matrix * Transform;
    }
    
    protected override void PopTransformCore() =>
        Transform = (_transforms ?? throw new InvalidOperationException()).Pop();

    protected override void PopClipCore()
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
    protected override void PopGeometryClipCore()
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

    protected override void PopBitmapBlendModeCore()
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

    protected override void PopOpacityCore()
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

    protected override void PopOpacityMaskCore()
    {
        if (!_needsToPopOpacityMask!.Pop())
            return;
        
        var next = NextDrawAs<OpacityMaskNode>();

        if (next == null || !next.Item.Equals(null, null))
        {
            Add(new OpacityMaskPopNode());
        }
        else
        {
            ++_drawOperationIndex;
        }
    }


    protected override void PushClipCore(Rect clip)
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

    protected override void PushClipCore(RoundedRect clip)
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

    protected override void PushGeometryClipCore(Geometry clip)
    {
        if (clip.PlatformImpl is null)
            return;

        var next = NextDrawAs<GeometryClipNode>();

        if (next == null || !next.Item.Equals(Transform, clip.PlatformImpl))
        {
            Add(new GeometryClipNode(Transform, clip.PlatformImpl));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }
    
    protected override void PushOpacityCore(double opacity, Rect bounds)
    {
        var next = NextDrawAs<OpacityNode>();

        if (next == null || !next.Item.Equals(opacity, bounds))
        {
            Add(new OpacityNode(opacity, bounds));
        }
        else
        {
            ++_drawOperationIndex;
        }
    }

    protected override void PushOpacityMaskCore(IBrush mask, Rect bounds)
    {
        var next = NextDrawAs<OpacityMaskNode>();

        bool needsToPop = true;
        if (next == null || !next.Item.Equals(mask, bounds))
        {
            var immutableMask = ConvertBrush(mask);
            if (immutableMask != null)
                Add(new OpacityMaskNode(immutableMask, bounds));
            else
                needsToPop = false;
        }
        else
        {
            ++_drawOperationIndex;
        }

        _needsToPopOpacityMask ??= OpacityMaskPopStackPool.Get();
        _needsToPopOpacityMask.Push(needsToPop);
    }

    /// <inheritdoc/>
    protected override void PushBitmapBlendModeCore(BitmapBlendingMode blendingMode)
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
    
    private IImmutableBrush? ConvertBrush(IBrush? brush)
    {
        if (brush is IMutableBrush mutable)
            return mutable.ToImmutable();
        if (brush is ISceneBrush sceneBrush)
            return sceneBrush.CreateContent();
        return (IImmutableBrush?)brush;
    }
}
