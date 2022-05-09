using System.Numerics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class CompositorDrawingContextProxy : IDrawingContextImpl
{
    private IDrawingContextImpl _impl;

    public CompositorDrawingContextProxy(IDrawingContextImpl impl)
    {
        _impl = impl;
    }

    public Matrix PreTransform { get; set; } = Matrix.Identity;
    
    public void Dispose()
    {
        _impl.Dispose();
    }

    Matrix _transform;    
    public Matrix Transform
    {
        get => _transform;
        set => _impl.Transform = PreTransform * (_transform = value);
    }

    public void Clear(Color color)
    {
        _impl.Clear(color);
    }

    public void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect,
        BitmapInterpolationMode bitmapInterpolationMode = BitmapInterpolationMode.Default)
    {
        _impl.DrawBitmap(source, opacity, sourceRect, destRect, bitmapInterpolationMode);
    }

    public void DrawBitmap(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
    {
        _impl.DrawBitmap(source, opacityMask, opacityMaskRect, destRect);
    }

    public void DrawLine(IPen pen, Point p1, Point p2)
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

    public void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
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

    public void PushOpacity(double opacity)
    {
        _impl.PushOpacity(opacity);
    }

    public void PopOpacity()
    {
        _impl.PopOpacity();
    }

    public void PushOpacityMask(IBrush mask, Rect bounds)
    {
        _impl.PushOpacityMask(mask, bounds);
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

    public void PushBitmapBlendMode(BitmapBlendingMode blendingMode)
    {
        _impl.PushBitmapBlendMode(blendingMode);
    }

    public void PopBitmapBlendMode()
    {
        _impl.PopBitmapBlendMode();
    }

    public void Custom(ICustomDrawOperation custom)
    {
        _impl.Custom(custom);
    }

    public Matrix CutTransform(Matrix4x4 transform) => new Matrix(transform.M11, transform.M12, transform.M21,
        transform.M22, transform.M41,
        transform.M42);
}