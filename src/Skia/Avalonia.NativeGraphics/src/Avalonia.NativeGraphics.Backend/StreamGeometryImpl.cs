using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.NativeGraphics.Backend
{
    internal class StreamGeometryImpl : GeometryImpl, IStreamGeometryImpl
    {
        public IStreamGeometryImpl Clone()
        {
            return new StreamGeometryImpl();
        }

        class Context : IStreamGeometryContextImpl
        {
            public void Dispose()
            {
                
            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
            {
                
            }

            public void BeginFigure(Point startPoint, bool isFilled = true)
            {
            }

            public void CubicBezierTo(Point point1, Point point2, Point point3)
            {
            }

            public void QuadraticBezierTo(Point control, Point endPoint)
            {
            }

            public void LineTo(Point point)
            {
            }

            public void EndFigure(bool isClosed)
            {
            }

            public void SetFillRule(FillRule fillRule)
            {
            }
        }

        public IStreamGeometryContextImpl Open()
        {
            return new Context();
        }
    }
}