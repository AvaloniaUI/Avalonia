using System;
using System.Buffers;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal partial class RenderDataStream
{
    private struct HitTestScope
    {
        public bool SavedLive;
        public bool RestorePoint;
        public Point SavedPoint;
    }

    public bool HitTest(Point point)
    {
        var reader = new RenderDataReader(_writer.Written);
        HitTestScope[]? rented = null;
        scoped Span<HitTestScope> scopes;
        if (_maxDepth <= MaxStackScopeDepth)
            scopes = stackalloc HitTestScope[_maxDepth];
        else
            scopes = rented = ArrayPool<HitTestScope>.Shared.Rent(_maxDepth);
        var depth = 0;
        var current = point;
        var live = true;
        try
        {
            while (!reader.IsAtEnd)
            {
                switch (reader.ReadOpcode())
                {
                    case RenderDataOpcode.DrawLine:
                    {
                        reader.ReadInt32();
                        var clientPen = (IPen?)_resources[reader.ReadInt32()];
                        var p1 = reader.ReadPoint();
                        var p2 = reader.ReadPoint();
                        if (live && HitTestLine(clientPen, p1, p2, current))
                            return true;
                        break;
                    }
                    case RenderDataOpcode.DrawRectangle:
                    {
                        var serverBrush = (IBrush?)_resources[reader.ReadInt32()];
                        reader.ReadInt32();
                        var clientPen = (IPen?)_resources[reader.ReadInt32()];
                        var rect = reader.ReadRoundedRect();
                        SkipBoxShadows(ref reader);
                        if (live && HitTestRectangle(serverBrush, clientPen, rect, current))
                            return true;
                        break;
                    }
                    case RenderDataOpcode.DrawEllipse:
                    {
                        var serverBrush = (IBrush?)_resources[reader.ReadInt32()];
                        reader.ReadInt32();
                        var clientPen = (IPen?)_resources[reader.ReadInt32()];
                        var rect = reader.ReadRect();
                        if (live && HitTestEllipse(serverBrush, clientPen, rect, current))
                            return true;
                        break;
                    }
                    case RenderDataOpcode.DrawGeometry:
                    {
                        var serverBrush = (IBrush?)_resources[reader.ReadInt32()];
                        reader.ReadInt32();
                        var clientPen = (IPen?)_resources[reader.ReadInt32()];
                        var geometry = (IGeometryImpl?)_resources[reader.ReadInt32()];
                        if (live && geometry != null &&
                            ((serverBrush != null && geometry.FillContains(current)) ||
                             (clientPen != null && geometry.StrokeContains(clientPen, current))))
                            return true;
                        break;
                    }
                    case RenderDataOpcode.DrawGlyphRun:
                    {
                        reader.ReadInt32();
                        var glyphRun = (IRef<IGlyphRunImpl>?)_resources[reader.ReadInt32()];
                        if (live && glyphRun != null && glyphRun.Item.Bounds.ContainsExclusive(current))
                            return true;
                        break;
                    }
                    case RenderDataOpcode.DrawBitmap:
                    {
                        reader.ReadInt32();
                        reader.ReadDouble();
                        reader.ReadRect();
                        var destRect = reader.ReadRect();
                        if (live && destRect.Contains(current))
                            return true;
                        break;
                    }
                    case RenderDataOpcode.DrawCustom:
                    {
                        var operation = (ICustomDrawOperation?)_resources[reader.ReadInt32()];
                        if (live && operation != null && operation.HitTest(current))
                            return true;
                        break;
                    }
                    case RenderDataOpcode.PushClip:
                    {
                        var clip = reader.ReadRoundedRect();
                        var scope = new HitTestScope { SavedLive = live };
                        if (live && !clip.Rect.Contains(current))
                            live = false;
                        scopes[depth++] = scope;
                        break;
                    }
                    case RenderDataOpcode.PushGeometryClip:
                    {
                        var geometry = (IGeometryImpl?)_resources[reader.ReadInt32()];
                        var scope = new HitTestScope { SavedLive = live };
                        if (live && geometry != null && !geometry.FillContains(current))
                            live = false;
                        scopes[depth++] = scope;
                        break;
                    }
                    case RenderDataOpcode.PushTransform:
                    {
                        var matrix = reader.ReadMatrix();
                        var scope = new HitTestScope { SavedLive = live };
                        if (live)
                        {
                            if (matrix.TryInvert(out var inverted))
                            {
                                scope.RestorePoint = true;
                                scope.SavedPoint = current;
                                current = current.Transform(inverted);
                            }
                            else
                                live = false;
                        }
                        scopes[depth++] = scope;
                        break;
                    }
                    case RenderDataOpcode.PushOpacity:
                    {
                        reader.ReadDouble();
                        scopes[depth++] = new HitTestScope { SavedLive = live };
                        break;
                    }
                    case RenderDataOpcode.PushOpacityMask:
                    {
                        reader.ReadInt32();
                        reader.ReadRect();
                        scopes[depth++] = new HitTestScope { SavedLive = live };
                        break;
                    }
                    case RenderDataOpcode.PushRenderOptions:
                    {
                        reader.ReadRenderOptions();
                        scopes[depth++] = new HitTestScope { SavedLive = live };
                        break;
                    }
                    case RenderDataOpcode.PushTextOptions:
                    {
                        reader.ReadTextOptions();
                        scopes[depth++] = new HitTestScope { SavedLive = live };
                        break;
                    }
                    case RenderDataOpcode.Pop:
                    {
                        var scope = scopes[--depth];
                        live = scope.SavedLive;
                        if (scope.RestorePoint)
                            current = scope.SavedPoint;
                        break;
                    }
                }
            }

            return false;
        }
        finally
        {
            if (rented != null)
                ArrayPool<HitTestScope>.Shared.Return(rented);
        }
    }

    private static void SkipBoxShadows(ref RenderDataReader reader)
    {
        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
            reader.ReadBoxShadow();
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
