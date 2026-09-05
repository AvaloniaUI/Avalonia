using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal partial class RenderDataStream
{
    internal struct HitTestScope
    {
        public bool SavedLive;
        public bool RestorePoint;
        public Point? SavedPoint;
    }

    internal struct HitTestVisitor : IRenderDataVisitor<HitTestScope>
    {
        public bool StopVisiting { get; private set; }
        public Geometry? CurrentGeometry
        {
            get => _currentGeometry;
            set
            {
                _currentGeometry = value;
                _renderedGeometry = GetRenderedGeometry(value, s_defaultStokePen);
            }
        }

        public bool HitFound;
        public IntersectionResult HitResult;
        private Geometry? _currentGeometry;
        private Geometry? _renderedGeometry;
        public Point? CurrentPoint;
        public bool Live;

        private Stack<Geometry>? _savedGeometries = null;

        public HitTestVisitor()
        {
            StopVisiting = false;
            Live = true;
        }

        public HitTestVisitor(Geometry geometry) : this()
        {
            HitResult = IntersectionResult.NotCalculated;
            CurrentGeometry = geometry;
            _savedGeometries = new Stack<Geometry>();
        }

        public HitTestVisitor(Point point) : this()
        {
            HitResult = IntersectionResult.NotCalculated;
            CurrentPoint = point;
        }

        private void Hit()
        {
            HitFound = true;
            StopVisiting = true;
        }

        private void Hit(IntersectionResult result)
        {
            HitResult = result;
            StopVisiting = true;
        }

        public void OnDrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2)
        {
            if (!Live)
                return;

            if (CurrentPoint is { } point && HitTestLine(clientPen, p1, p2, point))
                Hit();

            else if (_renderedGeometry is { } geometry && HitTestLine(clientPen, p1, p2, geometry) is { } intersectionDetail)
                Hit(intersectionDetail);
        }

        public void OnDrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect,
            BoxShadows boxShadows)
        {
            if (!Live)
                return;

            if (CurrentPoint is { } point && HitTestRectangle(serverBrush, clientPen, rect, point))
                Hit();

            else if (_renderedGeometry is { } geometry && HitTestRectangle(serverBrush, clientPen, rect, geometry) is { } intersectionDetail)
                Hit(intersectionDetail);
        }

        public void OnDrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect)
        {
            if (!Live)
                return;

            if (CurrentPoint is { } point && HitTestEllipse(serverBrush, clientPen, rect, point))
                Hit();

            else if (_renderedGeometry is { } geometry && HitTestEllipse(serverBrush, clientPen, rect, geometry) is { } intersectionDetail)
                Hit(intersectionDetail);
        }

        public void OnDrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IGeometryImpl? geometry)
        {
            if (!Live || geometry == null)
                return;

            if (CurrentPoint is { } point &&
                ((serverBrush != null && geometry.FillContains(point)) ||
                 (clientPen != null && geometry.StrokeContains(clientPen, point))))
                Hit();

            else if (_renderedGeometry is { } currentGeometry &&
                serverBrush != null && currentGeometry.PlatformImpl != null &&
                GetRenderedGeometry(geometry, clientPen) is { } renderedGeometry &&
                renderedGeometry.GetFillIntersectionResult(currentGeometry.PlatformImpl) is { } intersectionDetail)
                Hit(intersectionDetail);
        }

        public void OnDrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun)
        {
            if (!Live || glyphRun == null)
                return;

            if (CurrentPoint is { } point && glyphRun.Item.Bounds.ContainsExclusive(point))
                Hit();

            else if (_renderedGeometry is { } geometry && GetIntersectionDetail(glyphRun.Item.Bounds, geometry.Bounds) is { } intersectionDetail)
                Hit(intersectionDetail);
        }

        public void OnDrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect)
        {
            if (!Live)
                return;

            if (CurrentPoint is { } point && destRect.Contains(point))
                Hit();

            else if (_renderedGeometry is { } geometry && GetIntersectionDetail(destRect, geometry.Bounds) is { } intersectionDetail)
                Hit(intersectionDetail);
        }

        public void OnDrawCustom(ICustomDrawOperation? operation)
        {
            if (!Live)
                return;

            if (CurrentPoint is { } point && operation != null && operation.HitTest(point))
                Hit();

            else if (_renderedGeometry is { } geometry && operation != null && operation.HitTest(geometry) is { } intersectionDetail)
                Hit(intersectionDetail);
        }

        private IntersectionResult GetIntersectionDetail(Rect firstRect, Rect secondRect)
        {
            if (firstRect.Contains(secondRect))
                return IntersectionResult.FullyContains;

            if (secondRect.Contains(secondRect))
                return IntersectionResult.FullyInside;

            if (firstRect.Intersects(secondRect))
                return IntersectionResult.Intersects;

            return IntersectionResult.Empty;
        }

        public void OnDrawRecording(ServerCompositionRenderData? server, CompositionRenderData? client,
            RenderDataStream? stream, Matrix transform)
        {
            if (!Live)
                return;

            var inverted = Matrix.Identity;
            if (!transform.IsIdentity && !transform.TryInvert(out inverted))
                return;

            // Hit testing is a client-side query, so compositor-bound children
            // answer from their client data.
            if (CurrentPoint is { } point)
            {
                if (!transform.IsIdentity)
                    point = point.Transform(inverted);

                if (client != null ? client.HitTest(point) : stream?.HitTest(point) == true)
                    Hit();
            }
            else if (CurrentGeometry is { } currentGeometry)
            {
                var geometry = currentGeometry;
                if (!transform.IsIdentity)
                {
                    geometry = geometry.Clone();
                    geometry.Transform = new MatrixTransform((geometry.Transform?.Value ?? Matrix.Identity) * inverted);
                }

                // The child answers for all of its content, so its Empty must not
                // stop the walk - later ops in this stream can still intersect.
                var result = client != null
                    ? client.HitTest(geometry)
                    : stream?.HitTest(geometry) ?? IntersectionResult.Empty;
                if (result > IntersectionResult.Empty)
                    Hit(result);
            }
        }

        public HitTestScope OnPushClip(RoundedRect clip)
        {
            var scope = new HitTestScope { SavedLive = Live };

            if (Live)
            {
                if ((CurrentPoint is { } point && !clip.Rect.Contains(point)) ||
                    (_renderedGeometry is { } geometry && !clip.Rect.Contains(geometry.Bounds)))
                    Live = false;
            }

            return scope;
        }

        public HitTestScope OnPushGeometryClip(IGeometryImpl? geometry)
        {
            var scope = new HitTestScope { SavedLive = Live };
            if (Live && geometry != null)
            {
                if ((CurrentPoint is { } point && !geometry.FillContains(point)) || (_renderedGeometry is { } currentGeometry && currentGeometry.PlatformImpl != null &&
                geometry.GetFillIntersectionResult(currentGeometry.PlatformImpl) > IntersectionResult.Empty))
                    Live = false;
            }
            return scope;
        }

        public HitTestScope OnPushOpacity(double opacity)
            => new HitTestScope { SavedLive = Live };

        public HitTestScope OnPushOpacityMask(IBrush? brush, Rect bounds)
            => new HitTestScope { SavedLive = Live };

        public HitTestScope OnPushTransform(Matrix matrix)
        {
            var scope = new HitTestScope { SavedLive = Live };
            if (Live)
            {
                if (matrix.TryInvert(out var inverted))
                {
                    scope.RestorePoint = true;
                    if (CurrentPoint is { } point)
                    {
                        scope.SavedPoint = point;
                        CurrentPoint = point.Transform(inverted);
                    }
                    else if (CurrentGeometry != null)
                    {
                        _savedGeometries?.Push(CurrentGeometry);
                        CurrentGeometry = CurrentGeometry.Clone();
                        CurrentGeometry.Transform = new MatrixTransform((CurrentGeometry.Transform?.Value ?? Matrix.Identity) * inverted);
                        _renderedGeometry?.Transform = CurrentGeometry.Transform;
                    }
                }
                else
                    Live = false;
            }
            return scope;
        }

        public HitTestScope OnPushRenderOptions(RenderOptions options)
            => new HitTestScope { SavedLive = Live };

        public HitTestScope OnPushTextOptions(TextOptions options)
            => new HitTestScope { SavedLive = Live };

        public HitTestScope OnPushEffect(IEffect? effect, Rect bounds)
            => new HitTestScope { SavedLive = Live };

        public void OnPop(in HitTestScope scope)
        {
            Live = scope.SavedLive;
            if (scope.RestorePoint)
            {
                CurrentPoint = scope.SavedPoint;
                CurrentGeometry = _savedGeometries?.Pop();
            }
        }
    }

    public bool HitTest(Point point)
    {
        var visitor = new HitTestVisitor(point);
        Visit<HitTestVisitor, HitTestScope>(ref visitor);
        return visitor.HitFound;
    }

    public IntersectionResult HitTest(Geometry geometry)
    {
        var visitor = new HitTestVisitor(geometry);
        Visit<HitTestVisitor, HitTestScope>(ref visitor);
        return visitor.HitResult;
    }

    private static bool HitTestLine(IPen? clientPen, Point p1, Point p2, Point p)
    {
        if (clientPen == null)
            return false;

        var halfThickness = clientPen.Thickness / 2;
        var minX = Math.Min(p1.X, p2.X) - halfThickness;
        var maxX = Math.Max(p1.X, p2.X) + halfThickness;
        var minY = Math.Min(p1.Y, p2.Y) - halfThickness;
        var maxY = Math.Max(p1.Y, p2.Y) + halfThickness;

        if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
            return false;

        Vector ap = p - p1;
        var dot1 = Vector.Dot(p2 - p1, ap);
        if (dot1 < 0)
            return ap.Length <= halfThickness;

        Vector bp = p - p2;
        var dot2 = Vector.Dot(p1 - p2, bp);
        if (dot2 < 0)
            return bp.Length <= halfThickness;

        var bXaX = p2.X - p1.X;
        var bYaY = p2.Y - p1.Y;
        var distance = (bXaX * (p.Y - p1.Y) - bYaY * (p.X - p1.X)) /
                       Math.Sqrt(bXaX * bXaX + bYaY * bYaY);
        return Math.Abs(distance) <= halfThickness;
    }

    private static IntersectionResult? HitTestLine(IPen? clientPen, Point p1, Point p2, Geometry geometry)
    {
        if (clientPen == null)
            return IntersectionResult.NotCalculated;

        return geometry.GetFillIntersectionResult(new LineGeometry(p1, p2));
    }

    private static bool HitTestRectangle(IBrush? serverBrush, IPen? clientPen, RoundedRect rect, Point p)
    {
        var strokeThicknessAdjustment = (clientPen?.Thickness / 2) ?? 0;

        if (rect.IsRounded)
        {
            var outer = rect.Inflate(strokeThicknessAdjustment, strokeThicknessAdjustment);
            if (outer.ContainsExclusive(p))
            {
                if (serverBrush != null)
                    return true;

                var inner = rect.Deflate(strokeThicknessAdjustment, strokeThicknessAdjustment);
                return !inner.ContainsExclusive(p);
            }
        }
        else
        {
            var outer = rect.Rect.Inflate(strokeThicknessAdjustment);
            if (outer.ContainsExclusive(p))
            {
                if (serverBrush != null)
                    return true;

                var inner = rect.Rect.Deflate(strokeThicknessAdjustment);
                return !inner.ContainsExclusive(p);
            }
        }

        return false;
    }

    private static IntersectionResult? HitTestRectangle(IBrush? serverBrush, IPen? clientPen, RoundedRect rect, Geometry geometry)
    {
        var strokeThicknessAdjustment = (clientPen?.Thickness / 2) ?? 0;

        if (rect.IsRounded)
        {
            var outer = rect.Inflate(strokeThicknessAdjustment, strokeThicknessAdjustment);
            return new RectangleGeometry(outer.Rect, outer.RadiiTopLeft.X, outer.RadiiTopLeft.Y).GetFillIntersectionResult(geometry);
        }
        else
        {
            var outer = rect.Rect.Inflate(strokeThicknessAdjustment);
            return new RectangleGeometry(outer).GetFillIntersectionResult(geometry);
        }
    }

    private static readonly IPen s_defaultStokePen = new Pen();

    private static Geometry? GetRenderedGeometry(Geometry? geometry, IPen? pen)
    {
        return geometry == null ? null : new CombinedGeometry(geometry.GetWidenedGeometry(pen ?? s_defaultStokePen), geometry);
    }

    private static IGeometryImpl? GetRenderedGeometry(IGeometryImpl? geometry, IPen? pen)
    {
        return geometry == null ? null : new CombinedGeometry(new ImmutableGeometry(geometry.GetWidenedGeometry(pen ?? s_defaultStokePen)), 
            new ImmutableGeometry(geometry)).PlatformImpl;
    }

    private static bool HitTestEllipse(IBrush? serverBrush, IPen? clientPen, Rect rect, Point p)
    {
        var center = rect.Center;
        var strokeThickness = clientPen?.Thickness ?? 0;

        var rx = rect.Width / 2 + strokeThickness / 2;
        var ry = rect.Height / 2 + strokeThickness / 2;

        var dx = p.X - center.X;
        var dy = p.Y - center.Y;

        if (Math.Abs(dx) > rx || Math.Abs(dy) > ry)
            return false;

        if (serverBrush != null)
            return EllipseContains(dx, dy, rx, ry);

        if (strokeThickness > 0)
        {
            var inStroke = EllipseContains(dx, dy, rx, ry);

            rx = rect.Width / 2 - strokeThickness / 2;
            ry = rect.Height / 2 - strokeThickness / 2;

            var inInner = EllipseContains(dx, dy, rx, ry);

            return inStroke && !inInner;
        }

        return false;
    }

    private static IntersectionResult? HitTestEllipse(IBrush? serverBrush, IPen? clientPen, Rect rect, Geometry geometry)
    {
        var center = rect.Center;
        var strokeThickness = clientPen?.Thickness ?? 0;

        var rx = rect.Width / 2 + strokeThickness / 2;
        var ry = rect.Height / 2 + strokeThickness / 2;

        var ellipse = new EllipseGeometry(rect);

        return ellipse.GetFillIntersectionResult(geometry);
    }

    private static bool EllipseContains(double dx, double dy, double radiusX, double radiusY)
    {
        var rx2 = radiusX * radiusX;
        var ry2 = radiusY * radiusY;
        var distance = ry2 * dx * dx + rx2 * dy * dy;
        return distance <= rx2 * ry2;
    }
}
