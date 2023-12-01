using System;
using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataRectangleNode : RenderDataBrushAndPenNode
{
    public RoundedRect Rect { get; set; }
    public BoxShadows BoxShadows { get; set; }

    static bool IsOutsideCorner(double dx, double dy, double radius)
    {
        return (dx < 0) && (dy < 0) && (dx * dx + dy * dy > radius * radius);
    }

    bool RoundedRectContains(Rect bounds, Point p, double radiusAdjustment)
    {
        // Do a simple rectangular bounds check first
        if (!bounds.ContainsExclusive(p))
            return false;

        // Update the radii by the adjustment amount
        var rrect = new RoundedRect(
            Rect.Rect,
            new Vector(Math.Max(0, Rect.RadiiTopLeft.X + radiusAdjustment), Math.Max(0, Rect.RadiiTopLeft.Y + radiusAdjustment)),
            new Vector(Math.Max(0, Rect.RadiiTopRight.X + radiusAdjustment), Math.Max(0, Rect.RadiiTopRight.Y + radiusAdjustment)),
            new Vector(Math.Max(0, Rect.RadiiBottomRight.X + radiusAdjustment), Math.Max(0, Rect.RadiiBottomRight.Y + radiusAdjustment)),
            new Vector(Math.Max(0, Rect.RadiiBottomLeft.X + radiusAdjustment), Math.Max(0, Rect.RadiiBottomLeft.Y + radiusAdjustment))
            );

        // If any radii totals exceed available bounds, determine a scale factor that needs to be applied
        var scaleFactor = 1.0;
        if (bounds.Width > 0)
        {
            var radiiWidth = Math.Max(rrect.RadiiTopLeft.X + rrect.RadiiTopRight.X, rrect.RadiiBottomLeft.X + rrect.RadiiBottomRight.X);
            if (radiiWidth > bounds.Width)
                scaleFactor = Math.Min(scaleFactor, bounds.Width / radiiWidth);
        }
        if (bounds.Height > 0)
        {
            var radiiHeight = Math.Max(rrect.RadiiTopLeft.Y + rrect.RadiiBottomLeft.Y, rrect.RadiiTopRight.Y + rrect.RadiiBottomRight.Y);
            if (radiiHeight > bounds.Height)
                scaleFactor = Math.Min(scaleFactor, bounds.Height / radiiHeight);
        }

        // Before corner hit-testing, make the point relative to the bounds' upper-left
        p = new Point(p.X - bounds.X, p.Y - bounds.Y);

        // Top-left corner
        var radius = Math.Min(rrect.RadiiTopLeft.X, rrect.RadiiTopLeft.Y) * scaleFactor;
        if (IsOutsideCorner(p.X - radius, p.Y - radius, radius))
            return false;

        // Top-right corner
        radius = Math.Min(rrect.RadiiTopRight.X, rrect.RadiiTopRight.Y) * scaleFactor;
        if (IsOutsideCorner(bounds.Width - radius - p.X, p.Y - radius, radius))
            return false;

        // Bottom-right corner
        radius = Math.Min(rrect.RadiiBottomRight.X, rrect.RadiiBottomRight.Y) * scaleFactor;
        if (IsOutsideCorner(bounds.Width - radius - p.X, bounds.Height - radius - p.Y, radius))
            return false;

        // Bottom-left corner
        radius = Math.Min(rrect.RadiiBottomLeft.X, rrect.RadiiBottomLeft.Y) * scaleFactor;
        if (IsOutsideCorner(p.X - radius, bounds.Height - radius - p.Y, radius))
            return false;

        return true;
    }

    public override bool HitTest(Point p)
    {
        var strokeThicknessAdjustment = (ClientPen?.Thickness / 2) ?? 0;

        if (ServerBrush != null) // it's safe to check for null
        {
            var rect = Rect.Rect.Inflate(strokeThicknessAdjustment);

            if (Rect.IsRounded)
                return RoundedRectContains(rect, p, strokeThicknessAdjustment);
            else
                return rect.ContainsExclusive(p);
        }
        else
        {
            var borderRect = Rect.Rect.Inflate(strokeThicknessAdjustment);
            var emptyRect = Rect.Rect.Deflate(strokeThicknessAdjustment);

            if (Rect.IsRounded)
                return RoundedRectContains(borderRect, p, strokeThicknessAdjustment) && !RoundedRectContains(emptyRect, p, -strokeThicknessAdjustment);
            else
                return borderRect.ContainsExclusive(p) && !emptyRect.ContainsExclusive(p);
        }
    }

    public override void Invoke(ref RenderDataNodeRenderContext context) =>
        context.Context.DrawRectangle(ServerBrush, ServerPen, Rect, BoxShadows);

    public override Rect? Bounds => BoxShadows.TransformBounds(Rect.Rect).Inflate((ServerPen?.Thickness ?? 0) / 2);
}
