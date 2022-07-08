using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.NativeGraphics.Backend
{
    internal class GeometryImpl : IGeometryImpl, ITransformedGeometryImpl
    {
        private IGeometryImpl _sourceGeometry;
        private Matrix _transform;

        public Rect GetRenderBounds(IPen pen)
        {
            return new Rect();
        }

        public bool FillContains(Point point)
        {
            return false;
        }

        public IGeometryImpl Intersect(IGeometryImpl geometry)
        {
            return new GeometryImpl();
        }

        public bool StrokeContains(IPen pen, Point point)
        {
            return false;
        }

        public ITransformedGeometryImpl WithTransform(Matrix transform)
        {
            return new GeometryImpl
            {
                _sourceGeometry = this,
                _transform = transform
            };
        }

        public bool TryGetPointAtDistance(double distance, out Point point)
        {
            point = default;
            return false;
        }

        public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
        {
            point = default;
            tangent = default;
            return false;
        }

        public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure,
            out IGeometryImpl segmentGeometry)
        {
            segmentGeometry = null;
            return false;
        }

        public Rect Bounds { get; }
        public double ContourLength { get; }

        public IGeometryImpl SourceGeometry => _sourceGeometry;

        public Matrix Transform => _transform;
    }
}