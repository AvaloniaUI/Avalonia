using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal class RenderDataDrawingContext : DrawingContext
{
    private readonly Compositor? _compositor;
    private RenderDataStream? _stream;
    private CompositionRenderData? _renderData;
    private HashSet<object>? _resourcesHashSet;
    private Stack<PushEntry>? _pushStack;
    private static readonly ThreadSafeObjectPool<HashSet<object>> s_hashSetPool = new();
    private static readonly ThreadSafeObjectPool<Stack<PushEntry>> s_pushStackPool = new();

    private struct PushEntry
    {
        public bool Emitted;
        public int PositionBefore;
        public int PositionAfter;
        public int DepthBefore;
    }

    public RenderDataDrawingContext(Compositor? compositor)
    {
        _compositor = compositor;
    }

    private RenderDataStream Stream => _stream ??= new RenderDataStream();

    private CompositionRenderData RenderData => _renderData ??= new CompositionRenderData(_compositor!, Stream);

    private void AddResource(object? resource)
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

    private void PushedScope(int positionBefore) =>
        (_pushStack ??= s_pushStackPool.Get()).Push(new PushEntry
        {
            Emitted = true,
            PositionBefore = positionBefore,
            PositionAfter = Stream.OpcodeLength,
            DepthBefore = Stream.Depth - 1
        });

    private void PushedNoOpScope() =>
        (_pushStack ??= s_pushStackPool.Get()).Push(new PushEntry { Emitted = false });

    private void PopCore()
    {
        var entry = _pushStack!.Pop();
        if (!entry.Emitted)
            return;

        if (Stream.OpcodeLength == entry.PositionAfter)
            Stream.Rewind(entry.PositionBefore, entry.DepthBefore);
        else
            Stream.Pop();
    }

    protected override void DrawLineCore(IPen? pen, Point p1, Point p2)
    {
        if (pen == null)
            return;
        AddResource(pen);
        Stream.DrawLine(pen.GetServer(_compositor), pen, p1, p2);
    }

    protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry)
    {
        if (brush == null && pen == null)
            return;
        AddResource(brush);
        AddResource(pen);
        Stream.DrawGeometry(brush.GetServer(_compositor), pen.GetServer(_compositor), pen, geometry);
    }

    protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default)
    {
        if (rrect.IsEmpty())
            return;
        if (brush == null && pen == null && boxShadows == default)
            return;
        AddResource(brush);
        AddResource(pen);
        Stream.DrawRectangle(brush.GetServer(_compositor), pen.GetServer(_compositor), pen, rrect, boxShadows);
    }

    protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect)
    {
        if (rect.IsEmpty())
            return;
        if (brush == null && pen == null)
            return;
        AddResource(brush);
        AddResource(pen);
        Stream.DrawEllipse(brush.GetServer(_compositor), pen.GetServer(_compositor), pen, rect);
    }

    public override void Custom(ICustomDrawOperation custom) => Stream.DrawCustom(custom);

    public override void DrawGlyphRun(IBrush? foreground, GlyphRun? glyphRun)
    {
        if (foreground == null || glyphRun == null)
            return;
        AddResource(foreground);
        Stream.DrawGlyphRun(foreground.GetServer(_compositor), glyphRun.PlatformImpl.Clone());
    }

    internal override void DrawBitmap(IRef<IBitmapImpl>? source, double opacity, Rect sourceRect, Rect destRect)
    {
        if (source == null || sourceRect.IsEmpty() || destRect.IsEmpty())
            return;
        Stream.DrawBitmap(source.Clone(), opacity, sourceRect, destRect);
    }

    protected override void PushClipCore(RoundedRect rect)
    {
        var before = Stream.OpcodeLength;
        Stream.PushClip(rect);
        PushedScope(before);
    }

    protected override void PushClipCore(Rect rect)
    {
        var before = Stream.OpcodeLength;
        Stream.PushClip(new RoundedRect(rect));
        PushedScope(before);
    }

    protected override void PushGeometryClipCore(Geometry? clip)
    {
        if (clip == null)
        {
            PushedNoOpScope();
            return;
        }

        var before = Stream.OpcodeLength;
        Stream.PushGeometryClip(clip.PlatformImpl);
        PushedScope(before);
    }

    protected override void PushOpacityCore(double opacity)
    {
        if (opacity == 1)
        {
            PushedNoOpScope();
            return;
        }

        var before = Stream.OpcodeLength;
        Stream.PushOpacity(opacity);
        PushedScope(before);
    }

    protected override void PushOpacityMaskCore(IBrush? mask, Rect bounds)
    {
        if (mask == null)
        {
            PushedNoOpScope();
            return;
        }

        AddResource(mask);
        var before = Stream.OpcodeLength;
        Stream.PushOpacityMask(mask.GetServer(_compositor), bounds);
        PushedScope(before);
    }

    protected override void PushTransformCore(Matrix matrix)
    {
        if (matrix.IsIdentity)
        {
            PushedNoOpScope();
            return;
        }

        var before = Stream.OpcodeLength;
        Stream.PushTransform(matrix);
        PushedScope(before);
    }

    protected override void PushRenderOptionsCore(RenderOptions renderOptions)
    {
        var before = Stream.OpcodeLength;
        Stream.PushRenderOptions(renderOptions);
        PushedScope(before);
    }

    protected override void PushTextOptionsCore(TextOptions textOptions)
    {
        var before = Stream.OpcodeLength;
        Stream.PushTextOptions(textOptions);
        PushedScope(before);
    }

    protected override void PopClipCore() => PopCore();

    protected override void PopGeometryClipCore() => PopCore();

    protected override void PopOpacityCore() => PopCore();

    protected override void PopOpacityMaskCore() => PopCore();

    protected override void PopTransformCore() => PopCore();

    protected override void PopRenderOptionsCore() => PopCore();

    protected override void PopTextOptionsCore() => PopCore();

    private void FlushStack()
    {
        while (_pushStack is { Count: > 0 })
            PopCore();
    }

    public CompositionRenderData? GetRenderResults()
    {
        Debug.Assert(_compositor != null);
        FlushStack();

        var rv = _renderData;
        if (rv == null)
        {
            if (_stream is { OpcodeLength: > 0 })
                rv = new CompositionRenderData(_compositor!, _stream);
            else
            {
                _stream?.Dispose();
                _stream = null;
                return null;
            }
        }

        _renderData = null;
        _stream = null;
        _resourcesHashSet?.Clear();

        _compositor!.RegisterForSerialization(rv);
        return rv;
    }

    public ImmediateRenderDataSceneBrushContent? GetImmediateSceneBrushContent(ITileBrush brush, Rect? rect, bool useScalableRasterization)
    {
        Debug.Assert(_compositor == null);
        Debug.Assert(_renderData == null);
        FlushStack();

        if (_stream is not { OpcodeLength: > 0 })
        {
            _stream?.Dispose();
            _stream = null;
            return null;
        }

        var stream = _stream;
        _stream = null;
        return new ImmediateRenderDataSceneBrushContent(brush, stream, rect, useScalableRasterization);
    }

    public void Reset()
    {
        if (_renderData != null)
        {
            _renderData.Dispose();
            _renderData = null;
        }
        else
            _stream?.Dispose();

        _stream = null;
        _pushStack?.Clear();
        _resourcesHashSet?.Clear();
    }

    protected override void DisposeCore()
    {
        Reset();
        if (_resourcesHashSet != null)
            s_hashSetPool.ReturnAndSetNull(ref _resourcesHashSet);
        if (_pushStack != null)
            s_pushStackPool.ReturnAndSetNull(ref _pushStack);
    }
}
