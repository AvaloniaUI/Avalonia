// -----------------------------------------------------------------------
// <copyright file="Direct2DStreamGeometry.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;
    using Perspex.Media;
    using Perspex.Platform;
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

        public override Rect GetRenderBounds(double strokeThickness)
        {
            return geometry.GetWidenedBounds((float)strokeThickness).ToPerspex();
        }

        public IStreamGeometryContextImpl Open()
        {
            return new StreamGeometryContextImpl(geometry.Open());
        }
    }
}
