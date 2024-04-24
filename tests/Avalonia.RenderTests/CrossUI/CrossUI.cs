// ReSharper disable RedundantNameQualifier

#nullable enable
using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia;

namespace CrossUI;

public partial class CrossGlobals
{

}

public class CrossBrush
{
    public double Opacity = 1;
    public Avalonia.Matrix? Transform;
    public Avalonia.Matrix? RelativeTransform;
}

public class CrossSolidColorBrush : CrossBrush
{
    public Avalonia.Media.Color Color = Avalonia.Media.Colors.Black;

    public CrossSolidColorBrush()
    {
        
    }

    public CrossSolidColorBrush(Avalonia.Media.Color color)
    {
        Color = color;
    }
}

public class CrossGradientBrush : CrossBrush
{
    public List<GradientStop> GradientStops = new();
    public Avalonia.Media.GradientSpreadMethod SpreadMethod;
    public BrushMappingMode MappingMode;
}

public class CrossRadialGradientBrush : CrossGradientBrush
{
    public Avalonia.Point Center;
    public  Avalonia.Point GradientOrigin;
    public double RadiusX, RadiusY;
}

public class CrossTileBrush : CrossBrush
{
    public Avalonia.Media.AlignmentX AlignmentX = AlignmentX.Center;
    public Avalonia.Media.AlignmentY AlignmentY = AlignmentY.Center;
    public Avalonia.Media.Stretch Stretch = Stretch.Fill;
    public Avalonia.Media.TileMode TileMode  = TileMode.None;
    public Rect Viewbox = new Rect(0, 0, 1, 1);
    public Avalonia.Media.BrushMappingMode ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
    public Rect Viewport = new Rect(0, 0, 1, 1);
    public Avalonia.Media.BrushMappingMode ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
}


public abstract class CrossDrawing
{
}


public class CrossGeometryDrawing : CrossDrawing
{
    public CrossGeometry Geometry;
    public CrossBrush? Brush;
    public CrossPen? Pen;
    public CrossGeometryDrawing(CrossGeometry geometry)
    {
        Geometry = geometry;
    }
}

public class CrossDrawingGroup : CrossDrawing
{
    public List<CrossDrawing> Children = new();
}

public abstract class CrossGeometry
{
    
}

public class CrossSvgGeometry : CrossGeometry
{
    public string Path;

    public CrossSvgGeometry(string path)
    {
        Path = path;
    }
}

public class CrossEllipseGeometry : CrossGeometry
{
    public CrossEllipseGeometry(Rect rect)
    {
        Rect = rect;
    }

    public CrossEllipseGeometry()
    {
        
    }

    public Rect Rect { get; set; }
}

public class CrossStreamGeometry : CrossGeometry
{
    private ICrossStreamGeometryContextImpl? _contextImpl;

    public CrossStreamGeometry()
    {

    }

    public ICrossStreamGeometryContextImpl GetContext()
    {
        _contextImpl ??= CrossGlobals.GetContextImplProvider().Create();

        return _contextImpl;
    }
}

public class CrossRectangleGeometry : CrossGeometry
{
    public Rect Rect;

    public CrossRectangleGeometry(Rect rect)
    {
        Rect = rect;
    }
}

public class CrossDrawingBrush : CrossTileBrush
{
    public CrossDrawing Drawing;
}

public class CrossPen
{
    public CrossBrush Brush;
    public double Thickness = 1;
}

public interface ICrossStreamGeometryContextImpl : IDisposable
{
    object GetGeometry();
    void BeginFigure(Point point, bool isFilled, bool isClosed);
    void EndFigure();
    void LineTo(Point point, bool isStroked);
}

public interface ICrossStreamGeometryContextImplProvider
{
    ICrossStreamGeometryContextImpl Create();
}

public interface ICrossDrawingContext
{
    void DrawRectangle(CrossBrush? brush, CrossPen? pen, Rect rc);
    void DrawGeometry(CrossBrush? brush, CrossPen? pen, CrossGeometry geometry);
    void DrawImage(CrossImage image, Rect rc);
}

public abstract class CrossImage
{
    
}

public class CrossBitmapImage : CrossImage
{
    public string Path;
    public CrossBitmapImage(string path)
    {
        Path = path;
    }
}

public class CrossDrawingImage : CrossImage
{
    public CrossDrawing Drawing;
}


public class CrossControl
{
    public Rect Bounds => new Rect(Left, Top, Width, Height);
    public double Left, Top, Width, Height;
    public CrossBrush? Background;
    public CrossPen? Outline;
    public List<CrossControl> Children = new();
    public Matrix RenderTransform = Matrix.Identity;
    
    public virtual void Render(ICrossDrawingContext ctx)
    {
        var rc = new Rect(Bounds.Size);
        if (Background != null || Outline != null)
            ctx.DrawRectangle(Background, Outline, rc);
    }
}

public class CrossFuncControl : CrossControl
{
    private readonly Action<ICrossDrawingContext> _render;

    public CrossFuncControl(Action<ICrossDrawingContext> render)
    {
        _render = render;
    }

    public override void Render(ICrossDrawingContext ctx)
    {
        base.Render(ctx);
        _render(ctx);
    }
}

public class CrossImageControl : CrossControl
{
    public CrossImage Image;
    public override void Render(ICrossDrawingContext ctx)
    {
        base.Render(ctx);
        var rc = new Rect(Bounds.Size);
        ctx.DrawImage(Image, rc);
    }
}


