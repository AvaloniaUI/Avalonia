// ReSharper disable CompareOfFloatsByEqualityOperator

using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

/// <summary>
/// This struct is essentially the same thing as MilRectD
/// Unlike our "normal" Rect which is more human-readable and human-usable
/// this struct is optimized for actual processing that doesn't really care
/// about Width and Height but pretty much always only cares about
/// Right and Bottom edge coordinates
///
/// Not having to constantly convert between Width/Height and Right/Bottom for no actual reason
/// saves us some perf
///
/// This structure is intended to be mostly internal, but it's exposed as a PrivateApi type so it can
/// be passed to the drawing backend when needed
/// </summary>
[PrivateApi]
public struct LtrbRect
{
    public double Left, Top, Right, Bottom;

    internal LtrbRect(double x, double y, double right, double bottom)
    {
        Left = x;
        Top = y;
        Right = right;
        Bottom = bottom;
    }

    internal LtrbRect(Rect rc)
    {
        rc = rc.Normalize();
        Left = rc.X;
        Top = rc.Y;
        Right = rc.Right;
        Bottom = rc.Bottom;
    }

    internal bool IsZeroSize => Left == Right && Top == Bottom;

    internal LtrbRect Intersect(LtrbRect rect)
    {
        var newLeft = (rect.Left > Left) ? rect.Left : Left;
        var newTop = (rect.Top > Top) ? rect.Top : Top;
        var newRight = (rect.Right < Right) ? rect.Right : Right;
        var newBottom = (rect.Bottom < Bottom) ? rect.Bottom : Bottom;

        if ((newRight > newLeft) && (newBottom > newTop))
        {
            return new LtrbRect(newLeft, newTop, newRight, newBottom);
        }
        else
        {
            return default;
        }
    }

    internal bool Intersects(LtrbRect rect)
    {
        return (rect.Left < Right) && (Left < rect.Right) && (rect.Top < Bottom) && (Top < rect.Bottom);
    }

    internal Rect ToRect() => new(Left, Top, Right - Left, Bottom - Top);

    internal LtrbRect Inflate(Thickness thickness)
    {
        return new LtrbRect(Left - thickness.Left, Top - thickness.Top, Right + thickness.Right,
            Bottom + thickness.Bottom);
    }
    
    public static bool operator ==(LtrbRect left, LtrbRect right)=>
        left.Left == right.Left && left.Top == right.Top && left.Right == right.Right && left.Bottom == right.Bottom;

    public static bool operator !=(LtrbRect left, LtrbRect right) =>
        left.Left != right.Left || left.Top != right.Top || left.Right != right.Right || left.Bottom != right.Bottom;
    
    public bool Equals(LtrbRect other) =>
        other.Left == Left && other.Top == Top && other.Right == Right && other.Bottom == Bottom;
    
    public bool Equals(ref LtrbRect other) =>
        other.Left == Left && other.Top == Top && other.Right == Right && other.Bottom == Bottom;

    internal Point TopLeft => new Point(Left, Top);
    internal Point TopRight => new Point(Right, Top);
    internal Point BottomLeft => new Point(Left, Bottom);
    internal Point BottomRight => new Point(Right, Bottom);
    
    internal LtrbRect TransformToAABB(Matrix matrix)
    {
        ReadOnlySpan<Point> points = stackalloc Point[4]
        {
            TopLeft.Transform(matrix),
            TopRight.Transform(matrix),
            BottomRight.Transform(matrix),
            BottomLeft.Transform(matrix)
        };

        var left = double.MaxValue;
        var right = double.MinValue;
        var top = double.MaxValue;
        var bottom = double.MinValue;

        foreach (var p in points)
        {
            if (p.X < left) left = p.X;
            if (p.X > right) right = p.X;
            if (p.Y < top) top = p.Y;
            if (p.Y > bottom) bottom = p.Y;
        }

        return new LtrbRect(left, top, right, bottom);
    }
    
    /// <summary>
    /// Perform _WPF-like_ union operation
    /// </summary>
    private LtrbRect FullUnionCore(LtrbRect rect)
    {
        var x1 = Math.Min(Left, rect.Left);
        var x2 = Math.Max(Right, rect.Right);
        var y1 = Math.Min(Top, rect.Top);
        var y2 = Math.Max(Bottom, rect.Bottom);

        return new(x1, y1, x2, y2);
    }
    
    internal static LtrbRect? FullUnion(LtrbRect? left, LtrbRect? right)
    {
        if (left == null)
            return right;
        if (right == null)
            return left;
        return right.Value.FullUnionCore(left.Value);
    }
    
    internal static LtrbRect? FullUnion(LtrbRect? left, Rect? right)
    {
        if (right == null)
            return left;
        if (left == null)
            return new(right.Value);
        return left.Value.FullUnionCore(new(right.Value));
    }

    public override bool Equals(object? obj)
    {
        if (obj is LtrbRect other)
            return Equals(other);
        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 23) + Left.GetHashCode();
            hash = (hash * 23) + Top.GetHashCode();
            hash = (hash * 23) + Right.GetHashCode();
            hash = (hash * 23) + Bottom.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// This struct is essentially the same thing as RECT from win32 API
/// Unlike our "normal" PixelRect which is more human-readable and human-usable
/// this struct is optimized for actual processing that doesn't really care
/// about Width and Height but pretty much always only cares about
/// Right and Bottom edge coordinates
///
/// Not having to constantly convert between Width/Height and Right/Bottom for no actual reason
/// saves us some perf
///
/// This structure is intended to be mostly internal, but it's exposed as a PrivateApi type so it can
/// be passed to the drawing backend when needed
/// </summary>
[PrivateApi]
public struct LtrbPixelRect
{
    public int Left, Top, Right, Bottom;

    internal LtrbPixelRect(int x, int y, int right, int bottom)
    {
        Left = x;
        Top = y;
        Right = right;
        Bottom = bottom;
    }

    internal LtrbPixelRect(PixelSize size)
    {
        Left = 0;
        Top = 0;
        Right = size.Width;
        Bottom = size.Height;
    }

    internal bool IsEmpty => Left == Right && Top == Bottom;

    internal PixelRect ToPixelRect() => new(Left, Top, Right - Left, Bottom - Top);
    internal LtrbPixelRect Union(LtrbPixelRect rect)
    {
        if (IsEmpty)
            return rect;
        if (rect.IsEmpty)
            return this;
        var x1 = Math.Min(Left, rect.Left);
        var x2 = Math.Max(Right, rect.Right);
        var y1 = Math.Min(Top, rect.Top);
        var y2 = Math.Max(Bottom, rect.Bottom);

        return new(x1, y1, x2, y2);
    }

    internal Rect ToRectWithNoScaling() => new(Left, Top, (Right - Left), (Bottom - Top));

    internal bool Contains(int x, int y)
    {
        return x >= Left && x <= Right && y >= Top && y <= Bottom;
    }

    internal static LtrbPixelRect FromRectWithNoScaling(LtrbRect rect) =>
        new((int)rect.Left, (int)rect.Top, (int)Math.Ceiling(rect.Right),
            (int)Math.Ceiling(rect.Bottom));
    
    public static bool operator ==(LtrbPixelRect left, LtrbPixelRect right)=>
        left.Left == right.Left && left.Top == right.Top && left.Right == right.Right && left.Bottom == right.Bottom;

    public static bool operator !=(LtrbPixelRect left, LtrbPixelRect right) =>
        left.Left != right.Left || left.Top != right.Top || left.Right != right.Right || left.Bottom != right.Bottom;
    
    public bool Equals(LtrbPixelRect other) =>
        other.Left == Left && other.Top == Top && other.Right == Right && other.Bottom == Bottom;

    public override bool Equals(object? obj)
    {
        if (obj is LtrbPixelRect other)
            return Equals(other);
        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 23) + Left.GetHashCode();
            hash = (hash * 23) + Top.GetHashCode();
            hash = (hash * 23) + Right.GetHashCode();
            hash = (hash * 23) + Bottom.GetHashCode();
            return hash;
        }
    }

    internal Rect ToRectUnscaled() => new(Left, Top, Right - Left, Bottom - Top);

    internal static LtrbPixelRect FromRectUnscaled(LtrbRect rect)
    {
        return new LtrbPixelRect((int)rect.Left, (int)rect.Top, (int)Math.Ceiling(rect.Right),
            (int)Math.Ceiling(rect.Bottom));
    }
}