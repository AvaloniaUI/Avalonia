// -----------------------------------------------------------------------
// <copyright file="StreamGeometry.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using Splat;

    public class StreamGeometry : Geometry
    {
        public StreamGeometry()
        {
            this.PlatformImpl = Locator.Current.GetService<IStreamGeometryImpl>();
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

        public StreamGeometryContext Open()
        {
            return new StreamGeometryContext(((IStreamGeometryImpl)this.PlatformImpl).Open());
        }
    }
}
