// -----------------------------------------------------------------------
// <copyright file="StreamGeometry.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using Perspex.Platform;
    using Splat;

    /// <summary>
    /// Represents the geometry of an arbitrarily complex shape.
    /// </summary>
    public class StreamGeometry : Geometry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometry"/> class.
        /// </summary>
        public StreamGeometry()
        {
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();
            this.PlatformImpl = factory.CreateStreamGeometry();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometry"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific implementation.</param>
        private StreamGeometry(IGeometryImpl impl)
        {
            this.PlatformImpl = impl;
        }

        /// <inheritdoc/>
        public override Rect Bounds
        {
            get { return this.PlatformImpl.Bounds; }
        }

        /// <summary>
        /// Creates a <see cref="StreamGeometry"/> from a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>A <see cref="StreamGeometry"/>.</returns>
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

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new StreamGeometry(((IStreamGeometryImpl)this.PlatformImpl).Clone());
        }

        /// <summary>
        /// Opens the geometry to start defining it.
        /// </summary>
        /// <returns>
        /// A <see cref="StreamGeometryContext"/> which can be used to define the geometry.
        /// </returns>
        public StreamGeometryContext Open()
        {
            return new StreamGeometryContext(((IStreamGeometryImpl)this.PlatformImpl).Open());
        }
    }
}
