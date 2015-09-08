// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Cairo.Media
{
    using Cairo = global::Cairo;

    public class StreamGeometryContextImpl : IStreamGeometryContextImpl
    {
        private StreamGeometryImpl _impl;
        public StreamGeometryContextImpl(StreamGeometryImpl imp)
        {
            _impl = imp;
            _surf = new Cairo.ImageSurface(global::Cairo.Format.Argb32, 0, 0);
            _context = new Cairo.Context(_surf);
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            _context.MoveTo(startPoint.ToCairo());
        }

        public void BezierTo(Point point1, Point point2, Point point3)
        {
            _context.CurveTo(point1.ToCairo(), point2.ToCairo(), point3.ToCairo());
        }

        public void LineTo(Point point)
        {
            _context.LineTo(point.ToCairo());
        }

        private Cairo.Context _context;
        private Cairo.ImageSurface _surf;

        public void EndFigure(bool isClosed)
        {
            if (isClosed)
                _context.ClosePath();

            var extents = _context.StrokeExtents();
            _impl.Bounds = new Rect(extents.X, extents.Y, extents.Width, extents.Height);
            _impl.Path = _context.CopyPath();
        }

        public void Dispose()
        {
            _context.Dispose();
            _surf.Dispose();
        }
    }
}
