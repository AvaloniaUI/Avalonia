using System.Numerics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// A bunch of hacks to make the existing rendering operations and IDrawingContext
/// to work with composition rendering infrastructure.
/// 1) Keeps and applies the transform of the current visual since drawing operations think that
/// they have information about the full render transform (they are not)
/// 2) Keeps the draw list for the VisualBrush contents of the current drawing operation.
/// </summary>
internal class CompositorDrawingContextProxy : IDrawingContextImpl, IDrawingContextWithAcrylicLikeSupport
{
    private IDrawingContextImpl _impl;
    private readonly VisualBrushRenderer _visualBrushRenderer;

    public CompositorDrawingContextProxy(IDrawingContextImpl impl, VisualBrushRenderer visualBrushRenderer)
    {
        _impl = impl;
        _visualBrushRenderer = visualBrushRenderer;
    }

    // This is a hack to make it work with the current way of handling visual brushes
    public CompositionDrawList? VisualBrushDrawList
    {
        get => _visualBrushRenderer.VisualBrushDrawList;
        set => _visualBrushRenderer.VisualBrushDrawList = value;
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

    public class VisualBrushRenderer : IVisualBrushRenderer
    {
        public CompositionDrawList? VisualBrushDrawList { get; set; }
        public Size GetRenderTargetSize(IVisualBrush brush)
        {
            return VisualBrushDrawList?.Size ?? Size.Empty;
        }

        public void RenderVisualBrush(IDrawingContextImpl context, IVisualBrush brush)
        {
            if (VisualBrushDrawList != null)
            {
                foreach (var cmd in VisualBrushDrawList)
                    cmd.Item.Render(context);
            }
        }
    }

    public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
    {
        if (_impl is IDrawingContextWithAcrylicLikeSupport acrylic) 
            acrylic.DrawRectangle(material, rect);
    }
}
