using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
        public Point SavedPoint;
    }

    internal struct HitTestVisitor : IRenderDataVisitor<HitTestScope>
    {
        public bool StopVisiting { get; private set; }
        public bool HitFound;
        public Point Current;
        public bool Live;

        public HitTestVisitor(Point point)
        {
            StopVisiting = false;
            HitFound = false;
            Current = point;
            Live = true;
        }

        private void Hit()
        {
            HitFound = true;
            StopVisiting = true;
        }

        public void OnDrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2)
        {
            if (Live && HitTestLine(clientPen, p1, p2, Current))
                Hit();
        }

        public void OnDrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect,
            BoxShadows boxShadows)
        {
            if (Live && HitTestRectangle(serverBrush, clientPen, rect, Current))
                Hit();
        }

        public void OnDrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect)
        {
            if (Live && HitTestEllipse(serverBrush, clientPen, rect, Current))
                Hit();
        }

        public void OnDrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IGeometryImpl? geometry)
        {
            if (Live && geometry != null &&
                ((serverBrush != null && geometry.FillContains(Current)) ||
                 (clientPen != null && geometry.StrokeContains(clientPen, Current))))
                Hit();
        }

        public void OnDrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun)
        {
            if (Live && glyphRun != null && glyphRun.Item.Bounds.ContainsExclusive(Current))
                Hit();
        }

        public void OnDrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect)
        {
            if (Live && destRect.Contains(Current))
                Hit();
        }

        public void OnDrawCustom(ICustomDrawOperation? operation)
        {
            if (Live && operation != null && operation.HitTest(Current))
                Hit();
        }

        public HitTestScope OnPushClip(RoundedRect clip)
        {
            var scope = new HitTestScope { SavedLive = Live };
            if (Live && !clip.Rect.Contains(Current))
                Live = false;
            return scope;
        }

        public HitTestScope OnPushGeometryClip(IGeometryImpl? geometry)
        {
            var scope = new HitTestScope { SavedLive = Live };
            if (Live && geometry != null && !geometry.FillContains(Current))
                Live = false;
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
                    scope.SavedPoint = Current;
                    Current = Current.Transform(inverted);
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

        public void OnPop(in HitTestScope scope)
        {
            Live = scope.SavedLive;
            if (scope.RestorePoint)
                Current = scope.SavedPoint;
        }
    }

    public bool HitTest(Point point)
    {
        var visitor = new HitTestVisitor(point);
        Visit<HitTestVisitor, HitTestScope>(ref visitor);
        return visitor.HitFound;
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

    private static bool EllipseContains(double dx, double dy, double radiusX, double radiusY)
    {
        var rx2 = radiusX * radiusX;
        var ry2 = radiusY * radiusY;
        var distance = ry2 * dx * dx + rx2 * dy * dy;
        return distance <= rx2 * ry2;
    }
}
