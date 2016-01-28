// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Platform;

namespace Perspex.Media
{
    /// <summary>
    /// Represents the geometry of a line.
    /// </summary>
    public class LineGeometry : Geometry
    {
        private Point _startPoint;
        private Point _endPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGeometry"/> class.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        public LineGeometry(Point startPoint, Point endPoint)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            IPlatformRenderInterface factory = PerspexLocator.Current.GetService<IPlatformRenderInterface>();
            IStreamGeometryImpl impl = factory.CreateStreamGeometry();

            using (IStreamGeometryContextImpl context = impl.Open())
            {
                context.BeginFigure(_startPoint, false);
                context.LineTo(_endPoint);
                context.EndFigure(false);
            }

            PlatformImpl = impl;
        }

        /// <inheritdoc/>
        public override Rect Bounds
        {
            get
            {
                double xMin, yMin, xMax, yMax;
                if (_startPoint.X <= _endPoint.X)
                {
                    xMin = _startPoint.X;
                    xMax = _endPoint.X;
                }
                else
                {
                    xMin = _endPoint.X;
                    xMax = _startPoint.X;
                }
                if (_startPoint.Y <= _endPoint.Y)
                {
                    yMin = _startPoint.Y;
                    yMax = _endPoint.Y;
                }
                else
                {
                    yMin = _endPoint.Y;
                    yMax = _startPoint.Y;
                }

                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            }
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new LineGeometry(_startPoint, _endPoint);
        }
    }
}
