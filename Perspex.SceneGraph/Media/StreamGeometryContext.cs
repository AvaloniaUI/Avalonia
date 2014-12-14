// -----------------------------------------------------------------------
// <copyright file="StreamGeometryContext.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using Perspex.Platform;

    public class StreamGeometryContext : IDisposable
    {
        private IStreamGeometryContextImpl impl;

        public StreamGeometryContext(IStreamGeometryContextImpl impl)
        {
            this.impl = impl;
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            this.impl.BeginFigure(startPoint, isFilled);
        }

        public void BezierTo(Point point1, Point point2, Point point3)
        {
            this.impl.BezierTo(point1, point2, point3);
        }

        public void LineTo(Point point)
        {
            this.impl.LineTo(point);
        }

        public void EndFigure(bool isClosed)
        {
            this.impl.EndFigure(isClosed);
        }

        public void Dispose()
        {
            this.impl.Dispose();
        }
    }
}
