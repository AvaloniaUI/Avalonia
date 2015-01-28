// -----------------------------------------------------------------------
// <copyright file="StreamGeometryImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;
    using Perspex.Media;
    using Perspex.Platform;
    using SharpDX;
    using SharpDX.Direct2D1;
    using Splat;
    using D2DGeometry = SharpDX.Direct2D1.Geometry;

    public class StreamGeometryImpl : GeometryImpl, IStreamGeometryImpl
    {
        private PathGeometry path;

        public StreamGeometryImpl()
        {
            Factory factory = Locator.Current.GetService<Factory>();
            this.path = new PathGeometry(factory);
        }

        protected StreamGeometryImpl(PathGeometry geometry)
        {
            this.path = geometry;
        }

        public override Rect Bounds
        {
            get { return this.path.GetBounds().ToPerspex(); }
        }

        public override D2DGeometry DefiningGeometry
        {
            get { return this.path; }
        }

        public IStreamGeometryImpl Clone()
        {
            Factory factory = Locator.Current.GetService<Factory>();
            var result = new PathGeometry(factory);
            var sink = result.Open();
            this.path.Stream(sink);
            sink.Close();
            return new StreamGeometryImpl(result);
        }

        public IStreamGeometryContextImpl Open()
        {
            return new StreamGeometryContextImpl(this.path.Open());
        }
    }
}
