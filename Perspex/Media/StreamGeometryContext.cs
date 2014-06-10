// -----------------------------------------------------------------------
// <copyright file="StreamGeometryContext.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using Splat;

    public class StreamGeometryContext : Geometry
    {
        private IStreamGeometryContextImpl impl;

        public StreamGeometryContext(StreamGeometry geometry)
        {
            this.impl = Locator.Current.GetService<IStreamGeometryContextImpl>();
            this.impl.Initialize(geometry.Impl);
        }

        public void BeginFigure(Point startPoint, bool isFilled, bool isClosed)
        {
        }

        public void LineTo(Point point, bool isStroked, bool isSmoothJoin)
        {
        }
    }
}
