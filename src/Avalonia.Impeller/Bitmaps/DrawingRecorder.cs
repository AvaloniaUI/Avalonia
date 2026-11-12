using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Impeller;

/// <summary>Records drawing to replay later into any drawing context. Brushes and pens are snapshotted.</summary>
internal sealed class DrawingRecorder : IDrawingContextImpl, IDrawingContextImplWithEffects,
    IDrawingContextWithAcrylicLikeSupport
{
    private readonly List<Action<IDrawingContextImpl>> _commands = new();
    private readonly List<IDisposable> _resources = new();
    private readonly Action<DrawingRecorder>? _onDispose;
    private readonly Matrix _baseTransform;
    private Matrix _transform = Matrix.Identity;

    /// <param name="baseTransform">Applied after every transform, for example DPI scaling.</param>
    /// <param name="onDispose">Called when drawing is done.</param>
    public DrawingRecorder(Matrix baseTransform, Action<DrawingRecorder>? onDispose = null)
    {
        _baseTransform = baseTransform;
        _onDispose = onDispose;
        _commands.Add(c => c.Transform = baseTransform);
    }

    /// <summary>True if the recording starts by clearing everything, so earlier content can be dropped.</summary>
    public bool StartsWithClear { get; private set; }

    public bool IsEmpty => _commands.Count <= 1;

    public void Replay(IDrawingContextImpl target)
    {
        foreach (var command in _commands)
            command(target);
    }

    /// <summary>Releases scene brush content captured by the recording.</summary>
    public void Release()
    {
        foreach (var resource in _resources)
            resource.Dispose();
        _resources.Clear();
        _commands.Clear();
    }

    private void Add(Action<IDrawingContextImpl> command) => _commands.Add(command);

    private IBrush? Snapshot(IBrush? brush)
    {
        switch (brush)
        {
            case null:
                return null;
            case ISceneBrush scene:
                var content = scene.CreateContent();
                if (content is null)
                    return null;
                _resources.Add(content);
                return content;
            case ISceneBrushContent:
                return brush;
            default:
                return brush.ToImmutable();
        }
    }

    private IPen? Snapshot(IPen? pen) => pen?.ToImmutable();

    public Matrix Transform
    {
        get => _transform;
        set
        {
            _transform = value;
            var transform = value * _baseTransform;
            Add(c => c.Transform = transform);
        }
    }

    public void Clear(Color color)
    {
        if (IsEmpty && color.A == 0)
            StartsWithClear = true;
        Add(c => c.Clear(color));
    }

    public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect) =>
        Add(c => c.DrawBitmap(source, opacity, sourceRect, destRect));

    public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
    {
        var mask = Snapshot(opacityMask);
        if (mask is not null)
            Add(c => c.DrawBitmap(source, mask, opacityMaskRect, destRect));
    }

    public void DrawLine(IPen? pen, Point p1, Point p2)
    {
        var p = Snapshot(pen);
        Add(c => c.DrawLine(p, p1, p2));
    }

    public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry)
    {
        var b = Snapshot(brush);
        var p = Snapshot(pen);
        Add(c => c.DrawGeometry(b, p, geometry));
    }

    public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadows = default)
    {
        var b = Snapshot(brush);
        var p = Snapshot(pen);
        Add(c => c.DrawRectangle(b, p, rect, boxShadows));
    }

    public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
    {
        var m = material.ToImmutable();
        Add(c =>
        {
            if (c is IDrawingContextWithAcrylicLikeSupport acrylic)
                acrylic.DrawRectangle(m, rect);
            else
                c.DrawRectangle(new ImmutableSolidColorBrush(m.FallbackColor), null, rect);
        });
    }

    public void DrawRegion(IBrush? brush, IPen? pen, IPlatformRenderInterfaceRegion region)
    {
    }

    public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect)
    {
        var b = Snapshot(brush);
        var p = Snapshot(pen);
        Add(c => c.DrawEllipse(b, p, rect));
    }

    public void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun)
    {
        var f = Snapshot(foreground);
        Add(c => c.DrawGlyphRun(f, glyphRun));
    }

    public IDrawingContextLayerImpl CreateLayer(PixelSize size) => new ImpellerRecordedLayer(size, new Vector(96, 96));

    public void PushClip(Rect clip) => Add(c => c.PushClip(clip));

    public void PushClip(RoundedRect clip) => Add(c => c.PushClip(clip));

    public void PushClip(IPlatformRenderInterfaceRegion region) => Add(c => c.PushClip(region));

    public void PopClip() => Add(c => c.PopClip());

    public void PushLayer(Rect bounds) => Add(c => c.PushLayer(bounds));

    public void PopLayer() => Add(c => c.PopLayer());

    public void PushOpacity(double opacity, Rect? bounds) => Add(c => c.PushOpacity(opacity, bounds));

    public void PopOpacity() => Add(c => c.PopOpacity());

    public void PushOpacityMask(IBrush mask, Rect bounds)
    {
        var m = Snapshot(mask) ?? Brushes.Transparent;
        Add(c => c.PushOpacityMask(m, bounds));
    }

    public void PopOpacityMask() => Add(c => c.PopOpacityMask());

    public void PushGeometryClip(IGeometryImpl clip) => Add(c => c.PushGeometryClip(clip));

    public void PopGeometryClip() => Add(c => c.PopGeometryClip());

    public void PushRenderOptions(RenderOptions renderOptions) => Add(c => c.PushRenderOptions(renderOptions));

    public void PopRenderOptions() => Add(c => c.PopRenderOptions());

    public void PushTextOptions(TextOptions textOptions) => Add(c => c.PushTextOptions(textOptions));

    public void PopTextOptions() => Add(c => c.PopTextOptions());

    public void PushEffect(Rect? clipRect, IEffect effect)
    {
        var e = effect.ToImmutable();
        Add(c =>
        {
            if (c is IDrawingContextImplWithEffects effects)
                effects.PushEffect(clipRect, e);
            else
                c.PushLayer(clipRect ?? default);
        });
    }

    public void PopEffect() => Add(c =>
    {
        if (c is IDrawingContextImplWithEffects effects)
            effects.PopEffect();
        else
            c.PopLayer();
    });

    public object? GetFeature(Type t) => null;

    public void Dispose() => _onDispose?.Invoke(this);
}
