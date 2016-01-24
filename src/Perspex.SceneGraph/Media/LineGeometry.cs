// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Platform;
using System;

namespace Perspex.Media
{
    /// <summary>
    /// Represents the geometry of a line.
    /// </summary>
    public class LineGeometry : Geometry
    {
        private PointPair _pointPair;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGeometry"/> class.
        /// </summary>
        /// <param name="pointPair">The pointPair.</param>
        public LineGeometry(PointPair pointPair)
        {
            _pointPair = pointPair;
            IPlatformRenderInterface factory = PerspexLocator.Current.GetService<IPlatformRenderInterface>();
            IStreamGeometryImpl impl = factory.CreateStreamGeometry();

            using (IStreamGeometryContextImpl context = impl.Open())
            {
                context.BeginFigure(_pointPair.P1, false);
                context.LineTo(_pointPair.P2);
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
                if (_pointPair.P1.X <= _pointPair.P2.X)
                {
                    xMin = _pointPair.P1.X;
                    xMax = _pointPair.P2.X;
                }
                else
                {
                    xMin = _pointPair.P2.X;
                    xMax = _pointPair.P1.X;
                }
                if (_pointPair.P1.Y <= _pointPair.P2.Y)
                {
                    yMin = _pointPair.P1.Y;
                    yMax = _pointPair.P2.Y;
                }
                else
                {
                    yMin = _pointPair.P2.Y;
                    yMax = _pointPair.P1.Y;
                }

                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            }
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new LineGeometry(new PointPair(_pointPair.P1, _pointPair.P2));
        }
    }
}
