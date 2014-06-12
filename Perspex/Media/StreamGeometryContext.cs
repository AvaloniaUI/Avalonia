// -----------------------------------------------------------------------
// <copyright file="StreamGeometryContext.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using Splat;

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
