using System.Runtime.CompilerServices;

namespace Avalonia.Media
{
    internal static class RenderValidationExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRenderValid (this double d)
        {
            return !(double.IsNaN(d) || double.IsInfinity(d));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRenderValid (this Point p)
        {
            return p.X.IsRenderValid() && p.Y.IsRenderValid();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRenderValid (this Rect r)
        {
            return r.X.IsRenderValid() && r.Y.IsRenderValid() && r.Width.IsRenderValid() && r.Height.IsRenderValid();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRenderValid (this IPen p)
        {
            return p.Thickness.IsRenderValid() && p.MiterLimit.IsRenderValid();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRenderValid (this GlyphRun g)
        {
            return g.Bounds.IsRenderValid() && g.FontRenderingEmSize.IsRenderValid();
        }
    }
}
