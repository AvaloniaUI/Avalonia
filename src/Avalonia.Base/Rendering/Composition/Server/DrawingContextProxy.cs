using System;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// A bunch of hacks to make the existing rendering operations and IDrawingContext
/// to work with composition rendering infrastructure.
/// 1) Keeps and applies the transform of the current visual since drawing operations think that
/// they have information about the full render transform (they are not)
/// 2) Keeps the draw list for the VisualBrush contents of the current drawing operation.
/// </summary>
internal class CompositorDrawingContextProxy : IDrawingContextImpl,
    IDrawingContextWithAcrylicLikeSupport, IDrawingContextImplWithEffects
{
    private readonly IDrawingContextImpl _impl;

    public CompositorDrawingContextProxy(IDrawingContextImpl impl)
    {
        _impl = impl;
    }
    
    public Matrix PostTransform { get; set; } = Matrix.Identity;
    
    public void Dispose()
    {
        _impl.Dispose();
    }

    Matrix _transform;    
    public Matrix Transform
    {
        get => _transform;
        set => _impl.Transform = (_transform = value) * PostTransform;
    }

    public RenderOptions RenderOptions
    {
        get => _impl.RenderOptions;
        set => _impl.RenderOptions = value;
    }

    public void Clear(Color color)
    {
        _impl.Clear(color);
    }

    public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
    {
        _impl.DrawBitmap(source, opacity, sourceRect, destRect);
    }

    public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
    {
        _impl.DrawBitmap(source, opacityMask, opacityMaskRect, destRect);
    }

    public void DrawLine(IPen? pen, Point p1, Point p2)
    {
        _impl.DrawLine(pen, p1, p2);
    }

    public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry)
    {
        _impl.DrawGeometry(brush, pen, geometry);
    }

    public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadows = default)
    {
        _impl.DrawRectangle(brush, pen, rect, boxShadows);
    }

    public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect)
    {
        _impl.DrawEllipse(brush, pen, rect);
    }

    public void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun)
    {
        _impl.DrawGlyphRun(foreground, glyphRun);
    }

    public IDrawingContextLayerImpl CreateLayer(Size size)
    {
        return _impl.CreateLayer(size);
    }

    public void PushClip(Rect clip)
    {
        _impl.PushClip(clip);
    }

    public void PushClip(RoundedRect clip)
    {
        _impl.PushClip(clip);
    }

    public void PopClip()
    {
        _impl.PopClip();
    }

    public void PushOpacity(double opacity, Rect? bounds)
    {
        _impl.PushOpacity(opacity, bounds);
    }

    public void PopOpacity()
    {
        _impl.PopOpacity();
    }

    public void PushOpacityMask(IBrush mask, Rect bounds)
    {
        _impl.PushOpacityMask(mask, bounds);
    }

    public void PushRenderOptions(RenderOptions renderOptions)
    {
        _impl.PushRenderOptions(renderOptions);
    }

    public void PopOpacityMask()
    {
        _impl.PopOpacityMask();
    }

    public void PushGeometryClip(IGeometryImpl clip)
    {
        _impl.PushGeometryClip(clip);
    }

    public void PopGeometryClip()
    {
        _impl.PopGeometryClip();
    }

    public void PopRenderOptions()
    {
        _impl.PopRenderOptions();
    }

    public object? GetFeature(Type t) => _impl.GetFeature(t);
    

    public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
    {
        if (_impl is IDrawingContextWithAcrylicLikeSupport acrylic) 
            acrylic.DrawRectangle(material, rect);
        else
            _impl.DrawRectangle(new ImmutableSolidColorBrush(material.FallbackColor), null, rect);
    }

    public void PushEffect(IEffect effect)
    {
        if (_impl is IDrawingContextImplWithEffects effects)
            effects.PushEffect(effect);
    }

    public void PopEffect()
    {
        if (_impl is IDrawingContextImplWithEffects effects)
            effects.PopEffect();
    }
}
