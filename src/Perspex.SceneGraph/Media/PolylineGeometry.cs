using Perspex.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Media
{
    /// <summary>
    /// Represents the geometry of an polyline or polygon.
    /// </summary>
    public class PolylineGeometry : Geometry
    {
        private IList<Point> _points;
        private bool _isFilled;

        public PolylineGeometry(IList<Point> points, bool isFilled)
        {
            _points = points;
            _isFilled = isFilled;
            IPlatformRenderInterface factory = PerspexLocator.Current.GetService<IPlatformRenderInterface>();
            IStreamGeometryImpl impl = factory.CreateStreamGeometry();

            using (IStreamGeometryContextImpl context = impl.Open())
            {
                if (points.Count > 0)
                {
                    context.BeginFigure(points[0], isFilled);
                    for (int i = 1; i < points.Count; i++)
                    {
                        context.LineTo(points[i]);
                    }
                    context.EndFigure(isFilled);
                }
            }

            PlatformImpl = impl;
        }

        /// <inheritdoc/>
        public override Rect Bounds
        {
            get
            {
                double xMin = double.MaxValue, yMin = double.MaxValue;
                double xMax = double.MinValue, yMax = double.MinValue;
                foreach (var point in _points)
                {
                    if (point.X < xMin)
                    {
                        xMin = point.X;
                    }
                    else if (point.X > xMax)
                    {
                        xMax = point.X;
                    }

                    if (point.Y < yMin)
                    {
                        yMin = point.Y;
                    }
                    else if (point.Y > yMax)
                    {
                        yMax = point.Y;
                    }
                }

                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            }
        }
        public override Rect Bounds => PlatformImpl.Bounds;

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new PolylineGeometry(new List<Point>(_points), _isFilled);
        }
    }
}
