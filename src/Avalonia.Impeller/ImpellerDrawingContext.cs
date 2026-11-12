using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Impeller.Interop;
using Avalonia.Platform;
using Avalonia.Skia;
using NImpeller;
using SkiaSharp;

namespace Avalonia.Impeller;

/// <summary>Records Avalonia drawing into an Impeller display list and draws it into a surface on dispose.</summary>
internal sealed partial class ImpellerDrawingContext : IDrawingContextImpl, IDrawingContextImplWithEffects,
    IDrawingContextWithAcrylicLikeSupport
{
    private readonly ImpellerBackendContext _context;
    private readonly ImpellerSurface _surface;
    private readonly IDisposable? _session;
    private readonly bool _ownsSurface;
    private readonly bool _presentsSurface;
    private readonly Action? _onFlush;

    private ImpellerDisplayListBuilder _builder;
    private readonly PixelSize _size;
    // Applied after the Avalonia transform: a Y-flip for flipped surfaces, or DPI scaling.
    private Matrix _baseTransform;

    private Matrix _currentTransform = Matrix.Identity;
    private readonly Stack<(IBrush mask, Rect bounds)> _opacityMasks = new();
    private readonly Stack<Rect> _clipBounds = new();
    private readonly Stack<RenderOptions> _renderOptionsStack = new();
    private readonly List<IDisposable> _frameResources = new();

    // Solid-color paints reused across draws: the display list copies the paint when a draw is recorded.
    private ImpellerPaint? _solidFill;
    private ImpellerPaint? _solidStroke;
    private bool _solidFillLeased;
    private bool _solidStrokeLeased;

    /// <summary>On-screen context. Disposing the session presents, unless Impeller presents the surface.</summary>
    public ImpellerDrawingContext(ImpellerBackendContext context, ImpellerSurface surface,
        IDisposable session, PixelSize size, bool isYFlipped, bool presentsSurface = false)
        : this(context, surface, size, isYFlipped, ownsSurface: true, session, onFlush: null, presentsSurface)
    {
    }

    /// <summary>Offscreen context over a caller-owned surface. The prologue draws under everything else.</summary>
    internal ImpellerDrawingContext(ImpellerBackendContext context, ImpellerSurface surface, PixelSize size,
        Matrix baseTransform, Action<ImpellerDisplayListBuilder>? prologue, Action onFlush)
        : this(context, surface, size, baseTransform, ownsSurface: false, session: null, onFlush,
            presentsSurface: false)
    {
        prologue?.Invoke(_builder);
    }

    private ImpellerDrawingContext(ImpellerBackendContext context, ImpellerSurface surface, PixelSize size,
        bool isYFlipped, bool ownsSurface, IDisposable? session, Action? onFlush, bool presentsSurface)
        : this(context, surface, size, isYFlipped ? new Matrix(1, 0, 0, -1, 0, size.Height) : Matrix.Identity,
            ownsSurface, session, onFlush, presentsSurface)
    {
    }

    private ImpellerDrawingContext(ImpellerBackendContext context, ImpellerSurface surface, PixelSize size,
        Matrix baseTransform, bool ownsSurface, IDisposable? session, Action? onFlush, bool presentsSurface)
    {
        _context = context;
        _surface = surface;
        _size = size;
        _session = session;
        _ownsSurface = ownsSurface;
        _presentsSurface = presentsSurface;
        _onFlush = onFlush;

        var cull = new ImpellerRect { X = 0, Y = 0, Width = size.Width, Height = size.Height };
        _builder = ImpellerDisplayListBuilder.New(cull)
            ?? throw new InvalidOperationException("Failed to create an Impeller display-list builder.");

        _baseTransform = baseTransform;

        Transform = Matrix.Identity;
    }

    public Matrix Transform
    {
        get => _currentTransform;
        set
        {
            _currentTransform = value;
            _builder.SetTransform((value * _baseTransform).ToImpeller());
        }
    }

    public RenderOptions RenderOptions { get; set; }

    public void Clear(Color color)
    {
        using var paint = ImpellerPaint.New()!;
        paint.SetColor(color.ToImpeller());
        paint.SetBlendMode(ImpellerBlendMode.kImpellerBlendModeSource);
        _builder.DrawPaint(paint);
    }

    public void DrawLine(IPen? pen, Point p1, Point p2)
    {
        // DrawDashedLine only takes a single on/off pair.
        if (pen?.DashStyle is { Dashes.Count: > 2 } && ImpellerPaths.IsDashed(pen))
        {
            FillDashedLineOutline(p1, p2, pen);
            return;
        }

        using var stroke = ImpellerPaintBuilder.CreateStroke(pen, new Rect(p1, p2), this);
        if (stroke is not { } lease)
            return;

        if (pen?.DashStyle is { Dashes: { Count: > 0 } dashes })
        {
            var on = (float)(dashes[0] * pen.Thickness);
            var off = (float)((dashes.Count > 1 ? dashes[1] : dashes[0]) * pen.Thickness);
            _builder.DrawDashedLine(p1.ToImpellerPoint(), p2.ToImpellerPoint(), on, off, lease.Paint);
        }
        else
        {
            _builder.DrawLine(p1.ToImpellerPoint(), p2.ToImpellerPoint(), lease.Paint);
        }
    }

    public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry)
    {
        if (geometry is not GeometryImpl impl)
            return;

        var bounds = impl.Bounds;

        if (brush is not null && ImpellerPaths.Get(impl.FillPath) is { } fillPath)
        {
            using var fill = ImpellerPaintBuilder.CreateFill(brush, bounds, this);
            if (fill is { } fl)
                _builder.DrawPath(fillPath, fl.Paint);
        }

        if (pen is null || (impl.StrokePath ?? impl.FillPath) is not { } strokePath)
            return;

        if (ImpellerPaths.IsDashed(pen))
        {
            if (ImpellerPaths.GetDashedOutline(strokePath, pen) is not { } outline)
                return;
            using var outlineFill = ImpellerPaintBuilder.CreateFill(pen.Brush, bounds, this);
            if (outlineFill is { } ol)
                _builder.DrawPath(outline, ol.Paint);
            return;
        }

        if (ImpellerPaths.Get(strokePath) is not { } path)
            return;
        using var stroke = ImpellerPaintBuilder.CreateStroke(pen, bounds, this);
        if (stroke is { } sl)
            _builder.DrawPath(path, sl.Paint);
    }

    // The outline is cached at the origin and drawn translated, so moving shapes reuse it.
    private void DrawDashedOutline(RoundedRect shape, bool isOval, IPen pen)
    {
        var origin = shape.Rect.Position;
        var local = new Rect(shape.Rect.Size);
        var atOrigin = new RoundedRect(local, shape.RadiiTopLeft, shape.RadiiTopRight, shape.RadiiBottomRight,
            shape.RadiiBottomLeft);
        if (_context.DashedOutlines.Get(atOrigin, isOval, pen) is not { } outline)
            return;

        using var fill = ImpellerPaintBuilder.CreateFill(pen.Brush, local, this);
        if (fill is not { } fl)
            return;
        _builder.Save();
        _builder.Translate((float)origin.X, (float)origin.Y);
        _builder.DrawPath(outline, fl.Paint);
        _builder.Restore();
    }

    private void FillDashedLineOutline(Point p1, Point p2, IPen pen)
    {
        using var line = new SKPath();
        line.MoveTo(p1.ToSKPoint());
        line.LineTo(p2.ToSKPoint());
        using var outline = ImpellerPaths.CreateDashedOutline(line, pen);
        using var fill = ImpellerPaintBuilder.CreateFill(pen.Brush, new Rect(p1, p2), this);
        if (outline is not null && fill is { } fl)
            _builder.DrawPath(outline, fl.Paint);
    }

    public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadows = default)
    {
        var r = rect.Rect;

        DrawBoxShadows(rect, boxShadows);

        var impRect = r.ToImpeller();
        var radii = rect.IsRounded ? rect.ToImpeller() : default;

        using (var fill = ImpellerPaintBuilder.CreateFill(brush, r, this))
            if (fill is { } fl)
            {
                if (rect.IsRounded) _builder.DrawRoundedRect(impRect, radii, fl.Paint);
                else _builder.DrawRect(impRect, fl.Paint);
            }

        // Inset shadows are cast inward from the edges, so they draw on top of the fill.
        if (boxShadows.HasInsetShadows)
            DrawInsetBoxShadows(rect, boxShadows);

        if (pen is not null && ImpellerPaths.IsDashed(pen))
        {
            DrawDashedOutline(rect, isOval: false, pen);
            return;
        }

        using (var stroke = ImpellerPaintBuilder.CreateStroke(pen, r, this))
            if (stroke is { } sl)
            {
                if (rect.IsRounded) _builder.DrawRoundedRect(impRect, radii, sl.Paint);
                else _builder.DrawRect(impRect, sl.Paint);
            }
    }

    public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
    {
        var r = rect.Rect;
        if (r.Width <= 0 || r.Height <= 0)
            return;

        using var paint = ImpellerPaint.New()!;
        paint.SetColor(AcrylicColor(material).ToImpeller());
        if (rect.IsRounded) _builder.DrawRoundedRect(r.ToImpeller(), rect.ToImpeller(), paint);
        else _builder.DrawRect(r.ToImpeller(), paint);
    }

    // Tint over material color, as Skia's acrylic paint without the noise.
    internal static Color AcrylicColor(IExperimentalAcrylicMaterial material) =>
        CompositeOver(material.TintColor, material.MaterialColor);

    private static Color CompositeOver(Color src, Color dst)
    {
        var sa = src.A / 255.0;
        var da = dst.A / 255.0;
        var oa = sa + da * (1 - sa);
        if (oa <= 0)
            return Colors.Transparent;
        byte C(byte s, byte d) => (byte)Math.Clamp(Math.Round((s * sa + d * da * (1 - sa)) / oa), 0, 255);
        return new Color((byte)Math.Clamp(Math.Round(oa * 255), 0, 255), C(src.R, dst.R), C(src.G, dst.G), C(src.B, dst.B));
    }

    private void DrawBoxShadows(RoundedRect rect, BoxShadows boxShadows)
    {
        for (var i = 0; i < boxShadows.Count; i++)
        {
            var shadow = boxShadows[i];
            if (shadow.IsInset || shadow.Color.A == 0)
                continue;

            var shadowRect = rect.Rect
                .Inflate(shadow.Spread)
                .Translate(new Vector(shadow.OffsetX, shadow.OffsetY));

            using var paint = ImpellerPaint.New()!;
            paint.SetColor(shadow.Color.ToImpeller());
            if (shadow.Blur > 0)
            {
                using var blur = ImpellerMaskFilter.CreateBlurNew(
                    ImpellerBlurStyle.kImpellerBlurStyleNormal, (float)(shadow.Blur * 0.5));
                if (blur is not null)
                    paint.SetMaskFilter(blur);
                DrawShadowShape(shadowRect, rect, paint);
            }
            else
            {
                DrawShadowShape(shadowRect, rect, paint);
            }
        }
    }

    private void DrawShadowShape(Rect shadowRect, RoundedRect source, ImpellerPaint paint)
    {
        if (source.IsRounded)
            _builder.DrawRoundedRect(shadowRect.ToImpeller(), source.ToImpeller(), paint);
        else
            _builder.DrawRect(shadowRect.ToImpeller(), paint);
    }

    // Clip to the shape and draw the blurred area around it, as Skia does.
    private void DrawInsetBoxShadows(RoundedRect rect, BoxShadows boxShadows)
    {
        for (var i = 0; i < boxShadows.Count; i++)
        {
            var shadow = boxShadows[i];
            if (!shadow.IsInset || shadow.Color.A == 0)
                continue;

            using var paint = ImpellerPaint.New()!;
            paint.SetColor(shadow.Color.ToImpeller());
            using var blur = shadow.Blur > 0
                ? ImpellerMaskFilter.CreateBlurNew(ImpellerBlurStyle.kImpellerBlurStyleNormal, (float)(shadow.Blur * 0.5))
                : null;
            if (blur is not null)
                paint.SetMaskFilter(blur);

            var offset = new Vector(shadow.OffsetX, shadow.OffsetY);
            var outer = AreaCastingShadowInHole(rect.Rect, shadow.Blur, shadow.Spread, shadow.OffsetX, shadow.OffsetY);
            var inner = rect.Rect.Inflate(-shadow.Spread).Translate(offset);
            var innerRadii = rect.IsRounded ? rect.ToImpeller() : default;

            _builder.Save();
            if (rect.IsRounded)
                _builder.ClipRoundedRect(rect.Rect.ToImpeller(), rect.ToImpeller(),
                    ImpellerClipOperation.kImpellerClipOperationIntersect);
            else
                _builder.ClipRect(rect.Rect.ToImpeller(), ImpellerClipOperation.kImpellerClipOperationIntersect);
            _builder.DrawRoundedRectDifference(outer.ToImpeller(), default, inner.ToImpeller(), innerRadii, paint);
            _builder.Restore();
        }
    }

    // Adapted from Chromium (via Skia): the region an inset shadow's blur can reach inside the hole.
    private static Rect AreaCastingShadowInHole(Rect hole, double blur, double spread, double offsetX, double offsetY)
    {
        var bounds = hole.Inflate(blur);
        if (spread < 0)
            bounds = bounds.Inflate(-spread);
        var offsetBounds = bounds.Translate(new Vector(-offsetX, -offsetY));
        return bounds.Union(offsetBounds);
    }

    public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect)
    {
        var impRect = rect.ToImpeller();
        using (var fill = ImpellerPaintBuilder.CreateFill(brush, rect, this))
            if (fill is { } fl) _builder.DrawOval(impRect, fl.Paint);

        if (pen is not null && ImpellerPaths.IsDashed(pen))
        {
            DrawDashedOutline(new RoundedRect(rect), isOval: true, pen);
            return;
        }

        using (var stroke = ImpellerPaintBuilder.CreateStroke(pen, rect, this))
            if (stroke is { } sl) _builder.DrawOval(impRect, sl.Paint);
    }

    public void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun)
    {
        if (foreground is null || glyphRun is not GlyphRunImpl run || ImpellerGlyphRuns.GetOutline(run) is not { } outline)
            return;

        var origin = run.BaselineOrigin;
        _builder.Save();
        _builder.Translate((float)origin.X, (float)origin.Y);
        using (var fill = ImpellerPaintBuilder.CreateFill(foreground, run.Bounds.Translate(-(Vector)origin), this))
        {
            if (fill is { } fl)
                _builder.DrawPath(outline, fl.Paint);
        }
        _builder.Restore();
    }

    public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
    {
        if (source is IImpellerInlineBitmap inline)
        {
            inline.Draw(this, opacity, sourceRect, destRect);
            return;
        }

        if (ImpellerBitmapTextures.Get(source, _context) is not { } texture)
            return;

        var lent = RentSolidPaint(stroke: false);
        var paint = lent ?? ImpellerPaint.New()!;
        paint.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleFill);
        paint.SetColor(new ImpellerColor
        { Alpha = (float)opacity, Red = 1, Green = 1, Blue = 1, Color_space = ImpellerColorSpace.kImpellerColorSpaceSRGB });

        // Advanced blends need to read back the wrapped on-screen surface, which Impeller can't do.
        var blend = RenderOptions.BitmapBlendingMode.ToImpeller();
        if (blend > ImpellerBlendMode.kImpellerBlendModeModulate)
            blend = ImpellerBlendMode.kImpellerBlendModeSourceOver;
        paint.SetBlendMode(blend);

        var sampling = RenderOptions.BitmapInterpolationMode == BitmapInterpolationMode.None
            ? ImpellerTextureSampling.kImpellerTextureSamplingNearestNeighbor
            : ImpellerTextureSampling.kImpellerTextureSamplingLinear;

        _builder.DrawTextureRect(texture, sourceRect.ToImpeller(), destRect.ToImpeller(), sampling, paint);
        if (lent is not null)
            ReturnSolidPaint(stroke: false);
        else
            paint.Dispose();
    }

    public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
    {
        PushOpacityMask(opacityMask, opacityMaskRect);
        DrawBitmap(source, 1.0, new Rect(0, 0, source.PixelSize.Width, source.PixelSize.Height), destRect);
        PopOpacityMask();
    }

    public void DrawRegion(IBrush? brush, IPen? pen, IPlatformRenderInterfaceRegion region)
    {
    }

    public void PushClip(Rect clip)
    {
        _builder.Save();
        _builder.ClipRect(clip.ToImpeller(), ImpellerClipOperation.kImpellerClipOperationIntersect);
        PushClipBounds(clip);
    }

    public void PushClip(RoundedRect clip)
    {
        _builder.Save();
        if (clip.IsRounded)
            _builder.ClipRoundedRect(clip.Rect.ToImpeller(), clip.ToImpeller(),
                ImpellerClipOperation.kImpellerClipOperationIntersect);
        else
            _builder.ClipRect(clip.Rect.ToImpeller(), ImpellerClipOperation.kImpellerClipOperationIntersect);
        PushClipBounds(clip.Rect);
    }

    public void PushClip(IPlatformRenderInterfaceRegion region)
    {
        _builder.Save();
        _clipBounds.Push(CurrentClipDeviceBounds);
    }

    public void PopClip()
    {
        _builder.Restore();
        if (_clipBounds.Count > 0) _clipBounds.Pop();
    }

    public void PushGeometryClip(IGeometryImpl clip)
    {
        _builder.Save();
        if (clip is GeometryImpl impl && ImpellerPaths.Get(impl.FillPath) is { } path)
            _builder.ClipPath(path, ImpellerClipOperation.kImpellerClipOperationIntersect);
        PushClipBounds(clip.Bounds);
    }

    public void PopGeometryClip()
    {
        _builder.Restore();
        if (_clipBounds.Count > 0) _clipBounds.Pop();
    }

    private void PushClipBounds(Rect localClip)
    {
        var device = localClip.TransformToAABB(_currentTransform);
        _clipBounds.Push(CurrentClipDeviceBounds.Intersect(device));
    }

    /// <summary>The current clip in device pixels.</summary>
    internal Rect CurrentClipDeviceBounds => _clipBounds.Count > 0 ? _clipBounds.Peek() : FullBounds;

    public void PushOpacity(double opacity, Rect? bounds)
    {
        var layerBounds = (bounds ?? FullBounds).ToImpeller();
        using var paint = ImpellerPaint.New()!;
        paint.SetColor(new ImpellerColor
        { Alpha = (float)opacity, Red = 1, Green = 1, Blue = 1, Color_space = ImpellerColorSpace.kImpellerColorSpaceSRGB });
        _builder.SaveLayer(layerBounds, paint);
    }

    public void PopOpacity() => _builder.Restore();

    public void PushLayer(Rect bounds)
    {
        using var paint = ImpellerPaint.New()!;
        _builder.SaveLayer(bounds.ToImpeller(), paint);
    }

    public void PopLayer() => _builder.Restore();

    public void PushOpacityMask(IBrush mask, Rect bounds)
    {
        using var paint = ImpellerPaint.New()!;
        _builder.SaveLayer(bounds.ToImpeller(), paint);
        _opacityMasks.Push((mask, bounds));
    }

    public void PopOpacityMask()
    {
        if (_opacityMasks.Count == 0)
        {
            _builder.Restore();
            return;
        }

        var (mask, bounds) = _opacityMasks.Pop();
        using (var fill = ImpellerPaintBuilder.CreateFill(mask, bounds, this))
        {
            if (fill is { } fl)
            {
                fl.Paint.SetBlendMode(ImpellerBlendMode.kImpellerBlendModeDestinationIn);
                _builder.DrawRect(bounds.ToImpeller(), fl.Paint);
            }
        }
        _builder.Restore();
    }

    public IDrawingContextLayerImpl CreateLayer(PixelSize size) =>
        _context.CreateOffscreenRenderTarget(size, new Vector(1, 1), true);

    public void PushRenderOptions(RenderOptions renderOptions)
    {
        _renderOptionsStack.Push(RenderOptions);
        RenderOptions = RenderOptions.MergeWith(renderOptions);
    }

    public void PopRenderOptions() => RenderOptions = _renderOptionsStack.Pop();

    public void PushTextOptions(TextOptions textOptions) { }

    public void PopTextOptions() { }

    public object? GetFeature(Type t) => ImpellerSkiaLeaseFeature.Get(this, t);

    internal ImpellerBackendContext Backend => _context;

    internal ImpellerDisplayListBuilder Builder => _builder;

    /// <summary>Replays drawing made for a target of its own, mapped through <paramref name="mapping"/>.</summary>
    internal void DrawNested(Matrix mapping, Action<IDrawingContextImpl> draw)
    {
        var transform = _currentTransform;
        var baseTransform = _baseTransform;
        var clips = _clipBounds.Count;
        _baseTransform = mapping * transform * baseTransform;
        try
        {
            Transform = Matrix.Identity;
            draw(this);
        }
        finally
        {
            while (_clipBounds.Count > clips)
                _clipBounds.Pop();
            _baseTransform = baseTransform;
            Transform = transform;
        }
    }

    /// <summary>Draws a texture in device pixels. The resources are freed once the frame is done with them.</summary>
    internal void DrawDeviceTexture(ImpellerTexture texture, Rect deviceRect, IDisposable resources)
    {
        _frameResources.Add(resources);

        using var paint = ImpellerPaint.New()!;
        var src = new ImpellerRect { Width = (float)deviceRect.Width, Height = (float)deviceRect.Height };
        _builder.Save();
        _builder.SetTransform(_baseTransform.ToImpeller());
        _builder.DrawTextureRect(texture, src, deviceRect.ToImpeller(),
            ImpellerTextureSampling.kImpellerTextureSamplingLinear, paint);
        _builder.Restore();
    }

    private sealed class DisposeAction : IDisposable
    {
        private readonly Action _action;
        public DisposeAction(Action action) => _action = action;
        public void Dispose() => _action();
    }

    private Rect FullBounds => new(0, 0, _size.Width, _size.Height);

    /// <summary>Lends the reusable solid paint, or null if it is already lent.</summary>
    internal ImpellerPaint? RentSolidPaint(bool stroke)
    {
        if (stroke ? _solidStrokeLeased : _solidFillLeased)
            return null;
        ref var paint = ref stroke ? ref _solidStroke : ref _solidFill;
        paint ??= ImpellerPaint.New();
        if (paint is not null)
        {
            if (stroke)
                _solidStrokeLeased = true;
            else
                _solidFillLeased = true;
        }
        return paint;
    }

    internal void ReturnSolidPaint(bool stroke)
    {
        if (stroke)
            _solidStrokeLeased = false;
        else
            _solidFillLeased = false;
    }

    public void Dispose()
    {
        using (_context.EnterNativeScope())
            Flush();
    }

    private void Flush()
    {
        using var displayList = _builder.CreateDisplayListNew();
        try
        {
            if (displayList is not null)
                _surface.DrawDisplayList(displayList);
        }
        finally
        {
            _builder.Dispose();
            _solidFill?.Dispose();
            _solidStroke?.Dispose();

            // Freed a frame late, once the GPU is done with them.
            if (_frameResources.Count > 0)
                _context.AddFrameResources(_frameResources);
            _frameResources.Clear();

            if (_session is not null)
            {
                if (_presentsSurface)
                    _surface.Present();
                if (_ownsSurface)
                    _surface.Dispose();
                _session.Dispose();

                _context.EndFrame();
            }
            else
            {
                _onFlush?.Invoke();
            }
        }
    }
}
