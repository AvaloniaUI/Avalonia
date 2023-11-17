using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Media;

internal sealed class PlatformDrawingContext : DrawingContext
{
    private readonly IDrawingContextImpl _impl;
    private readonly bool _ownsImpl;
    private static ThreadSafeObjectPool<Stack<Matrix>> TransformStackPool { get; } =
        ThreadSafeObjectPool<Stack<Matrix>>.Default;

    private Stack<Matrix>? _transforms;
        

    public PlatformDrawingContext(IDrawingContextImpl impl, bool ownsImpl = true)
    {
        _impl = impl;
        _ownsImpl = ownsImpl;
    }

    public RenderOptions RenderOptions
    {
        get => _impl.RenderOptions;
        set => _impl.RenderOptions = value;
    }

    protected override void DrawLineCore(IPen pen, Point p1, Point p2) =>
        _impl.DrawLine(pen, p1, p2);

    protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry) =>
        _impl.DrawGeometry(brush, pen, geometry);

    protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect,
        BoxShadows boxShadows = default) =>
        _impl.DrawRectangle(brush, pen, rrect, boxShadows);

    protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect) => _impl.DrawEllipse(brush, pen, rect);

    internal override void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect) =>
        _impl.DrawBitmap(source.Item, opacity, sourceRect, destRect);

    public override void Custom(ICustomDrawOperation custom)
    {
        using var immediateDrawingContext = new ImmediateDrawingContext(_impl, false);
        try
        {
            custom.Render(immediateDrawingContext);
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)
                ?.Log(custom, $"Exception in {custom.GetType().Name}.{nameof(ICustomDrawOperation.Render)} {{0}}", e);
        }
    }

    public override void DrawGlyphRun(IBrush? foreground, GlyphRun glyphRun)
    {
        _ = glyphRun ?? throw new ArgumentNullException(nameof(glyphRun));

        if (foreground != null) 
            _impl.DrawGlyphRun(foreground, glyphRun.PlatformImpl.Item);
    }

    protected override void PushClipCore(RoundedRect rect) => _impl.PushClip(rect);

    protected override void PushClipCore(Rect rect) => _impl.PushClip(rect);

    protected override void PushGeometryClipCore(Geometry clip) =>
        _impl.PushGeometryClip(clip.PlatformImpl ?? throw new ArgumentException());

    protected override void PushOpacityCore(double opacity) => 
        _impl.PushOpacity(opacity, null);

    protected override void PushOpacityMaskCore(IBrush mask, Rect bounds) =>
        _impl.PushOpacityMask(mask, bounds);

    protected override void PushTransformCore(Matrix matrix)
    {
        _transforms ??= TransformStackPool.Get();
        var current = _impl.Transform;
        _transforms.Push(current);
        _impl.Transform = matrix * current;
    }

    protected override void PushRenderOptionsCore(RenderOptions renderOptions) => _impl.PushRenderOptions(renderOptions);

    protected override void PopClipCore() => _impl.PopClip();

    protected override void PopGeometryClipCore() => _impl.PopGeometryClip();

    protected override void PopOpacityCore() => _impl.PopOpacity();

    protected override void PopOpacityMaskCore() => _impl.PopOpacityMask();

    protected override void PopTransformCore() =>
        _impl.Transform =
            (_transforms ?? throw new ObjectDisposedException(nameof(PlatformDrawingContext))).Pop();

    protected override void PopRenderOptionsCore() => _impl.PopRenderOptions();

    protected override void DisposeCore()
    {
        if (_ownsImpl)
            _impl.Dispose();
        if (_transforms != null)
        {
            if (_transforms.Count != 0)
                throw new InvalidOperationException("Not all states are disposed");
            TransformStackPool.ReturnAndSetNull(ref _transforms);
        }
    }

    public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
    {
        if (_impl is IDrawingContextWithAcrylicLikeSupport idc)
            idc.DrawRectangle(material, rect);
        else
            DrawRectangle(new ImmutableSolidColorBrush(material.FallbackColor), null, rect);
    }
}
