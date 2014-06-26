// -----------------------------------------------------------------------
// <copyright file="StreamGeometry.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using Perspex.Platform;
    using Splat;

    public class StreamGeometry : Geometry
    {
        public StreamGeometry()
        {
            IPlatformInterface factory = Locator.Current.GetService<IPlatformInterface>();
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

        public StreamGeometryContext Open()
        {
            return new StreamGeometryContext(((IStreamGeometryImpl)this.PlatformImpl).Open());
        }
    }
}
