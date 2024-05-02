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

/// </summary>
internal partial class CompositorDrawingContextProxy : IDrawingContextImpl,
    IDrawingContextWithAcrylicLikeSupport, IDrawingContextImplWithEffects
{
    private readonly IDrawingContextImpl _impl;

    public CompositorDrawingContextProxy(IDrawingContextImpl impl)
    {
        _impl = impl;
    }
    
    public void Dispose()
    {
        Flush();
        _commands.Dispose();
    }

    public Matrix? PostTransform { get; set; }
    Matrix _transform;

    public Matrix Transform
    {
        get => _transform;
        set
        {
            _transform = value;
            SetTransform(value);
        }
    }

    void SetImplTransform(Matrix m)
    {
        _transform = m;
        if (PostTransform.HasValue)
            m = m * PostTransform.Value;
        _impl.Transform = m;
    }

    public void Clear(Color color)
    {
        Flush();
        _impl.Clear(color);
    }

    public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
    {
        Flush();
        _impl.DrawBitmap(source, opacity, sourceRect, destRect);
    }

    public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
    {
        Flush();
        _impl.DrawBitmap(source, opacityMask, opacityMaskRect, destRect);
    }

    public void DrawLine(IPen? pen, Point p1, Point p2)
    {
        Flush();
        _impl.DrawLine(pen, p1, p2);
    }

    public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry)
    {
        Flush();
        _impl.DrawGeometry(brush, pen, geometry);
    }

    public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadows = default)
    {
        Flush();
        _impl.DrawRectangle(brush, pen, rect, boxShadows);
    }

    public void DrawRegion(IBrush? brush, IPen? pen, IPlatformRenderInterfaceRegion region)
    {
        Flush();
        _impl.DrawRegion(brush, pen, region);
    }

    public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect)
    {
        Flush();
        _impl.DrawEllipse(brush, pen, rect);
    }

    public void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun)
    {
        Flush();
        _impl.DrawGlyphRun(foreground, glyphRun);
    }

    public IDrawingContextLayerImpl CreateLayer(PixelSize size)
    {
        return _impl.CreateLayer(size);
    }

    public void PushClip(Rect clip)
    {
        AddCommand(new()
        {
            Type = PendingCommandType.PushClip,
            DataUnion =
            {
                NormalRect = clip
            }
        });
    }

    public void PushClip(RoundedRect clip)
    {
        AddCommand(new()
        {
            Type = PendingCommandType.PushClip,
            DataUnion =
            {
                IsRoundRect = true,
                RoundRect = clip
            }
        });
    }

    public void PushClip(IPlatformRenderInterfaceRegion region)
    {
        Flush();
        _impl.PushClip(region);
    }

    public void PopClip()
    {
        if (!TryDiscard(PendingCommandType.PushClip)) 
            _impl.PopClip();
    }

    public void PushLayer(Rect bounds)
    {
        Flush();
        _impl.PushLayer(bounds);
    }

    public void PopLayer()
    {
        Flush();
        _impl.PopLayer();
    }

    public void PushOpacity(double opacity, Rect? bounds)
    {
        AddCommand(new PendingCommand
        {
            Type = PendingCommandType.PushOpacity,
            DataUnion =
            {
                Opacity = opacity,
                NullableOpacityRect = bounds
            }
        });
    }

    public void PopOpacity()
    {
        if (!TryDiscard(PendingCommandType.PushOpacity)) 
            _impl.PopOpacity();
    }

    public void PushOpacityMask(IBrush mask, Rect bounds)
    {
        AddCommand(new()
        {
            Type = PendingCommandType.PushOpacityMask,
            DataUnion =
            {
                NormalRect = bounds
            },
            ObjectUnion =
            {
                Mask = mask
            }
        });
    }

    public void PopOpacityMask()
    {
        if (!TryDiscard(PendingCommandType.PushOpacityMask)) 
            _impl.PopOpacityMask();
    }

    public void PushGeometryClip(IGeometryImpl clip)
    {
        AddCommand(new PendingCommand
        {
            Type = PendingCommandType.PushGeometryClip,
            ObjectUnion =
            {
                Clip = clip
            }
        });
    }

    public void PopGeometryClip()
    {
        if (!TryDiscard(PendingCommandType.PushGeometryClip)) 
            _impl.PopGeometryClip();
    }
    
    public void PushRenderOptions(RenderOptions renderOptions)
    {
        AddCommand(new()
        {
            Type = PendingCommandType.PushRenderOptions,
            DataUnion =
            {
                RenderOptions = renderOptions
            }
        });
    }

    public void PopRenderOptions()
    {
        if (!TryDiscard(PendingCommandType.PushRenderOptions)) 
            _impl.PopRenderOptions();
    }

    public object? GetFeature(Type t)
    {
        Flush();
        return _impl.GetFeature(t);
    }


    public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
    {
        Flush();
        if (_impl is IDrawingContextWithAcrylicLikeSupport acrylic) 
            acrylic.DrawRectangle(material, rect);
        else
            _impl.DrawRectangle(new ImmutableSolidColorBrush(material.FallbackColor), null, rect);
    }

    public void PushEffect(IEffect effect)
    {
        AddCommand(new()
        {
            Type = PendingCommandType.PushEffect,
            ObjectUnion =
            {
                Effect = effect
            }
        });
    }

    public void PopEffect()
    {
        if (!TryDiscard(PendingCommandType.PushEffect) && _impl is IDrawingContextImplWithEffects effects) 
            effects.PopEffect();
    }
}
