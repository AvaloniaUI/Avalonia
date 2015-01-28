// -----------------------------------------------------------------------
// <copyright file="StreamGeometry.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using Perspex.Platform;
    using Splat;

    public class StreamGeometry : Geometry
    {
        public StreamGeometry()
        {
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();
            this.PlatformImpl = factory.CreateStreamGeometry();
        }

        public override Rect Bounds
        {
            get { return this.PlatformImpl.Bounds; }
        }

        public static StreamGeometry Parse(string s)
        {
            StreamGeometry result = new StreamGeometry();

            using (StreamGeometryContext ctx = result.Open())
            {
                PathMarkupParser parser = new PathMarkupParser(result, ctx);
                parser.Parse(s);
                return result;
            }
        }

        private StreamGeometry(IGeometryImpl impl)
        {
            this.PlatformImpl = impl;
        }

        public override Geometry Clone()
        {
            return new StreamGeometry(((IStreamGeometryImpl)this.PlatformImpl).Clone());
        }

        public StreamGeometryContext Open()
        {
            return new StreamGeometryContext(((IStreamGeometryImpl)this.PlatformImpl).Open());
        }
    }
}
