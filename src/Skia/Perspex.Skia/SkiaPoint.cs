using System.Runtime.InteropServices;

namespace Perspex.Skia
{
    [StructLayout(LayoutKind.Sequential)]
    struct SkiaPoint
    {
        public float X, Y;

        public SkiaPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public SkiaPoint(double x, double y)
        {
            X = (float)x;
            Y = (float)y;
        }

        public SkiaPoint(Point p) : this(p.X, p.Y)
        {
            
        }

        public static implicit operator SkiaPoint(Point pt) => new SkiaPoint(pt);
    }
}