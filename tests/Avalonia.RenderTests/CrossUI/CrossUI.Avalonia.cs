#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using CrossUI;

namespace CrossUI
{
    using Avalonia;

    public partial class CrossGlobals
    {
        public static ICrossStreamGeometryContextImplProvider GetContextImplProvider()
        {
            return new AvaloniaCrossStreamGeometryContextImplProvider();
        }
    }

    public class AvaloniaCrossStreamGeometryContextImplProvider : ICrossStreamGeometryContextImplProvider
    {
        public ICrossStreamGeometryContextImpl Create()
        {
            return new AvaloniaCrossStreamGeometryContextImpl();
        }
    }

    public class AvaloniaCrossStreamGeometryContextImpl : ICrossStreamGeometryContextImpl
    {
        private StreamGeometry _streamGeometry;
        private StreamGeometryContext _context;
        private bool _isClosed;

        public AvaloniaCrossStreamGeometryContextImpl()
        {
            _streamGeometry = new StreamGeometry();
            _context = _streamGeometry.Open();
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked)
        {
            _context.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked);
        }

        public void BeginFigure(Point point, bool isFilled, bool isClosed)
        {
            _isClosed = isClosed;
            _context.BeginFigure(point, isFilled);
        }

        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked)
        {
            _context.CubicBezierTo(controlPoint1, controlPoint2, endPoint, isStroked);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public void EndFigure()
        {
            _context.EndFigure(_isClosed);
            Dispose();
        }

        public object GetGeometry()
        {
            return _streamGeometry;
        }

        public void LineTo(Point point, bool isStroked)
        {
            _context.LineTo(point, isStroked);
        }

        public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked)
        {
            _context.QuadraticBezierTo(controlPoint, endPoint, isStroked);
        }
    }
}

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests.CrossUI
{
#else
namespace Avalonia.Direct2D1.RenderTests.CrossUI
{
#endif

    class AvaloniaCrossControl : Control
    {
        private readonly CrossControl _src;
        private readonly Dictionary<CrossControl, AvaloniaCrossControl> _children;

        public AvaloniaCrossControl(CrossControl src)
        {
            _src = src;
            _children = src.Children.ToDictionary(x => x, x => new AvaloniaCrossControl(x));
            Width = src.Bounds.Width;
            Height = src.Bounds.Height;
            RenderTransform = new MatrixTransform(src.RenderTransform);
            RenderTransformOrigin = new RelativePoint(default, RelativeUnit.Relative);
            foreach (var ch in src.Children)
            {
                var c = _children[ch];
                VisualChildren.Add(c);
                LogicalChildren.Add(c);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var ch in _children)
                ch.Value.Measure(ch.Key.Bounds.Size);
            return _src.Bounds.Size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var ch in _children)
                ch.Value.Arrange(ch.Key.Bounds);
            return finalSize;
        }

        public override void Render(DrawingContext context)
        {
            _src.Render(new AvaloniaCrossDrawingContext(context));
        }
    }

    class AvaloniaCrossDrawingContext : ICrossDrawingContext
    {
        private readonly DrawingContext _ctx;

        public AvaloniaCrossDrawingContext(DrawingContext ctx)
        {
            _ctx = ctx;
        }

        static Transform? ConvertTransform(Matrix? m) => m == null ? null : new MatrixTransform(m.Value);

        static RelativeRect ConvertRect(Rect rc, BrushMappingMode mode)
            => new RelativeRect(rc,
                mode == BrushMappingMode.RelativeToBoundingBox ? RelativeUnit.Relative : RelativeUnit.Absolute);

        static RelativePoint ConvertPoint(Point pt, BrushMappingMode mode)
            => new(pt, mode == BrushMappingMode.RelativeToBoundingBox ? RelativeUnit.Relative : RelativeUnit.Absolute);

        static RelativeScalar ConvertScalar(double scalar, BrushMappingMode mode)
            => new(scalar, mode == BrushMappingMode.RelativeToBoundingBox ? RelativeUnit.Relative : RelativeUnit.Absolute);

        static Geometry ConvertGeometry(CrossGeometry g)
        {
            if (g is CrossRectangleGeometry rg)
                return new RectangleGeometry(rg.Rect);
            else if (g is CrossSvgGeometry svg)
                return PathGeometry.Parse(svg.Path);
            else if (g is CrossEllipseGeometry ellipse)
                return new EllipseGeometry(ellipse.Rect);
            else if(g is CrossStreamGeometry streamGeometry)
                return (StreamGeometry)streamGeometry.GetContext().GetGeometry();
            else if (g is CrossPathGeometry path)
                return new PathGeometry()
                {
                    Figures = RetAddRange(new PathFigures(), path.Figures.Select(f =>
                        new PathFigure()
                        {
                            StartPoint = f.Start,
                            IsClosed = f.Closed,
                            Segments = RetAddRange<PathSegments, PathSegment>(new PathSegments(), f.Segments.Select<CrossPathSegment, PathSegment>(s =>
                                s switch
                                {
                                    CrossPathSegment.Line l => new LineSegment()
                                    {
                                        Point = l.To, IsStroked = l.IsStroked
                                    },
                                    CrossPathSegment.Arc a => new ArcSegment()
                                    {
                                        Point = a.Point,
                                        RotationAngle = a.RotationAngle,
                                        Size = a.Size,
                                        IsLargeArc = a.IsLargeArc,
                                        SweepDirection = a.SweepDirection,
                                        IsStroked = a.IsStroked
                                    },
                                    CrossPathSegment.CubicBezier c => new BezierSegment()
                                    {
                                        Point1 = c.Point1,
                                        Point2 = c.Point2,
                                        Point3 = c.Point3,
                                        IsStroked = c.IsStroked
                                    },
                                    CrossPathSegment.QuadraticBezier q => new QuadraticBezierSegment()
                                    {
                                        Point1 = q.Point1,
                                        Point2 = q.Point2,
                                        IsStroked = q.IsStroked
                                    },
                                    CrossPathSegment.PolyLine p => new PolyLineSegment()
                                    {
                                        Points = p.Points.ToList(),
                                        IsStroked = p.IsStroked
                                    },
                                    _ => throw new InvalidOperationException()
                                }))
                        }))
                };
            throw new NotSupportedException();
        }

        static TList RetAddRange<TList, T>(TList l, IEnumerable<T> en) where TList : IList<T>
        {
            foreach(var e in en)
                l.Add(e);
            return l;
        }
    
        static Drawing ConvertDrawing(CrossDrawing src)
        {
            if (src is CrossDrawingGroup g)
                return new DrawingGroup() { Children = new DrawingCollection(g.Children.Select(ConvertDrawing)) };
            if (src is CrossGeometryDrawing geo)
                return new GeometryDrawing()
                {
                    Geometry = ConvertGeometry(geo.Geometry), Brush = ConvertBrush(geo.Brush), Pen = ConvertPen(geo.Pen)
                };
            throw new NotSupportedException();
        }
    
        static IBrush? ConvertBrush(CrossBrush? brush)
        {
            if (brush == null)
                return null;
            static Brush Sync(Brush dst, CrossBrush src)
            {
                dst.Opacity = src.Opacity;
                dst.Transform = ConvertTransform(src.Transform);
                dst.TransformOrigin = new RelativePoint(default, RelativeUnit.Absolute);
                if (src.RelativeTransform != null)
                    throw new PlatformNotSupportedException();
                return dst;
            }

            static Brush SyncTile(TileBrush dst, CrossTileBrush src)
            {
                dst.Stretch = src.Stretch;
                dst.AlignmentX = src.AlignmentX;
                dst.AlignmentY = src.AlignmentY;
                dst.TileMode = src.TileMode;
                dst.SourceRect = ConvertRect(src.Viewbox, src.ViewboxUnits);
                dst.DestinationRect = ConvertRect(src.Viewport, src.ViewportUnits);
                return Sync(dst, src);
            }

            static Brush SyncGradient(GradientBrush dst, CrossGradientBrush src)
            {
                dst.GradientStops = new GradientStops();
                dst.GradientStops.AddRange(src.GradientStops);
                dst.SpreadMethod = src.SpreadMethod;
                return Sync(dst, src);
            }
        
            if (brush is CrossSolidColorBrush br)
                return Sync(new SolidColorBrush(br.Color), brush);
            if (brush is CrossDrawingBrush db)
                return SyncTile(new DrawingBrush(ConvertDrawing(db.Drawing)), db);
            if (brush is CrossRadialGradientBrush radial)
                return SyncGradient(
                    new RadialGradientBrush()
                    {
                        Center = ConvertPoint(radial.Center, radial.MappingMode),
                        GradientOrigin = ConvertPoint(radial.GradientOrigin, radial.MappingMode),
                        RadiusX = ConvertScalar(radial.RadiusX, radial.MappingMode),
                        RadiusY = ConvertScalar(radial.RadiusY, radial.MappingMode)
                    }, radial);
            throw new NotSupportedException();
        }

        static IPen? ConvertPen(CrossPen? pen)
        {
            if (pen == null)
                return null;
            return new Pen(ConvertBrush(pen.Brush), pen.Thickness) { LineCap = pen.LineCap, LineJoin = pen.LineJoin };
        }

        static IImage ConvertImage(CrossImage image)
        {
            if (image is CrossBitmapImage bi)
                return new Bitmap(bi.Path);
            if (image is CrossDrawingImage di)
                return new DrawingImage(ConvertDrawing(di.Drawing));
            throw new NotSupportedException();
        }
    
        public void DrawRectangle(CrossBrush? brush, CrossPen? pen, Rect rc) => _ctx.DrawRectangle(ConvertBrush(brush), ConvertPen(pen), rc);
        public void DrawGeometry(CrossBrush? brush, CrossPen? pen, CrossGeometry geometry) =>
            _ctx.DrawGeometry(ConvertBrush(brush), ConvertPen(pen), ConvertGeometry(geometry));

        public void DrawImage(CrossImage image, Rect rc) => _ctx.DrawImage(ConvertImage(image), rc);
    }
}
