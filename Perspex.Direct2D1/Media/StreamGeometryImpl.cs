// -----------------------------------------------------------------------
// <copyright file="Direct2DStreamGeometry.cs" company="Tricycle">
// Copyright 2014 Tricycle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;
    using Perspex.Media;
    using SharpDX.Direct2D1;
    using Splat;

    public class StreamGeometryImpl : GeometryImpl, IStreamGeometryImpl
    {
        private PathGeometry geometry;

        public StreamGeometryImpl()
        {
            Factory factory = Locator.Current.GetService<Factory>();
            this.geometry = new PathGeometry(factory);
            this.Geometry = this.geometry;
        }

        public override Rect Bounds
        {
            get { return geometry.GetBounds().ToPerspex(); }
        }

        public IStreamGeometryContextImpl Open()
        {
            return new StreamGeometryContextImpl(geometry.Open());
        }
    }
}
