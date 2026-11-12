using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace RendererBench.Timing;

/// <summary>
/// Forwards to the backend's window drawing context and reports when drawing ends.
/// </summary>
internal sealed class TimingDrawingContext : IDrawingContextImplWithEffects
{
    private readonly IDrawingContextImpl _inner;
    private readonly Action<long> _onDispose;

    public TimingDrawingContext(IDrawingContextImpl inner, Action<long> onDispose)
    {
        _inner = inner;
        _onDispose = onDispose;
    }

    public Matrix Transform
    {
        get => _inner.Transform;
        set => _inner.Transform = value;
    }

    public void Dispose() => _onDispose(Stopwatch.GetTimestamp());

    public void Clear(Color color) => _inner.Clear(color);

    public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect) =>
        _inner.DrawBitmap(Unwrap(source), opacity, sourceRect, destRect);

    public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect) =>
        _inner.DrawBitmap(Unwrap(source), opacityMask, opacityMaskRect, destRect);

    public void DrawLine(IPen? pen, Point p1, Point p2) => _inner.DrawLine(pen, p1, p2);

    public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry) =>
        _inner.DrawGeometry(brush, pen, geometry);

    public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadows = default) =>
        _inner.DrawRectangle(brush, pen, rect, boxShadows);

    public void DrawRegion(IBrush? brush, IPen? pen, IPlatformRenderInterfaceRegion region) =>
        _inner.DrawRegion(brush, pen, region);

    public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect) => _inner.DrawEllipse(brush, pen, rect);

    public void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun) =>
        _inner.DrawGlyphRun(foreground, glyphRun);

    public IDrawingContextLayerImpl CreateLayer(PixelSize size) => new TimingLayer(_inner.CreateLayer(size));

    public void PushClip(Rect clip) => _inner.PushClip(clip);

    public void PushClip(RoundedRect clip) => _inner.PushClip(clip);

    public void PushClip(IPlatformRenderInterfaceRegion region) => _inner.PushClip(region);

    public void PopClip() => _inner.PopClip();

    public void PushLayer(Rect bounds) => _inner.PushLayer(bounds);

    public void PopLayer() => _inner.PopLayer();

    public void PushOpacity(double opacity, Rect? bounds) => _inner.PushOpacity(opacity, bounds);

    public void PopOpacity() => _inner.PopOpacity();

    public void PushOpacityMask(IBrush mask, Rect bounds) => _inner.PushOpacityMask(mask, bounds);

    public void PopOpacityMask() => _inner.PopOpacityMask();

    public void PushGeometryClip(IGeometryImpl clip) => _inner.PushGeometryClip(clip);

    public void PopGeometryClip() => _inner.PopGeometryClip();

    public void PushRenderOptions(RenderOptions renderOptions) => _inner.PushRenderOptions(renderOptions);

    public void PopRenderOptions() => _inner.PopRenderOptions();

    public void PushTextOptions(TextOptions textOptions) => _inner.PushTextOptions(textOptions);

    public void PopTextOptions() => _inner.PopTextOptions();

    public void PushEffect(Rect? clipRect, IEffect effect) =>
        ((IDrawingContextImplWithEffects)_inner).PushEffect(clipRect, effect);

    public void PopEffect() => ((IDrawingContextImplWithEffects)_inner).PopEffect();

    public object? GetFeature(Type t) => _inner.GetFeature(t);

    private static IBitmapImpl Unwrap(IBitmapImpl bitmap) => bitmap is TimingLayer layer ? layer.Inner : bitmap;

    /// <summary>
    /// Backends cast the target context in <see cref="IDrawingContextLayerImpl.Blit"/>, so the layer hands
    /// them the unwrapped one.
    /// </summary>
    private sealed class TimingLayer : IDrawingContextLayerImpl
    {
        public TimingLayer(IDrawingContextLayerImpl inner)
        {
            Inner = inner;
        }

        public IDrawingContextLayerImpl Inner { get; }

        public Vector Dpi => Inner.Dpi;

        public PixelSize PixelSize => Inner.PixelSize;

        public int Version => Inner.Version;

        public bool CanBlit => Inner.CanBlit;

        public bool IsCorrupted => Inner.IsCorrupted;

        public void Blit(IDrawingContextImpl context) =>
            Inner.Blit(context is TimingDrawingContext timing ? timing._inner : context);

        public IDrawingContextImpl CreateDrawingContext() => Inner.CreateDrawingContext();

        public void Save(Stream stream, BitmapEncoderOptions options) => Inner.Save(stream, options);

        public void Dispose() => Inner.Dispose();
    }
}
